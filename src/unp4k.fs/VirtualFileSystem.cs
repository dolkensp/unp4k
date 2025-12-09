using DokanNet;
using LTRData.Extensions.Native.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Security.AccessControl;
using System.Security.Principal;

namespace unp4k.fs
{
	internal class VirtualFileSystem : IDokanOperations2
	{
		public int DirectoryListingTimeoutResetIntervalMs => 30000;

		private readonly VirtualNode _rootNode;
		public DateTime Timestamp { get; set; } = DateTime.MinValue;
		public String VolumeLabel { get; set; } = "Virtual File System";
		public String FileSystemName { get; set; } = "Virtual File System";
		public Int64 VolumeSize { get; set; } = 0;

		public VirtualFileSystem(VirtualNode rootNode) => this._rootNode = rootNode;

		public void Cleanup(ReadOnlyNativeMemory<char> fileNamePtr, ref DokanFileInfo info)
		{

		}

		public void CloseFile(ReadOnlyNativeMemory<char> fileNamePtr, ref DokanFileInfo info)
		{
			
		}

		public NtStatus CreateFile(ReadOnlyNativeMemory<char> fileNamePtr, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, ref DokanFileInfo info)
		{

			String path = new String(fileNamePtr.Span);
			String trimmedPath = path.Trim('\\', '/');

			// Root
			if (String.IsNullOrEmpty(trimmedPath))
			{
				info.IsDirectory = true;
				info.Context = this._rootNode; // root node
				return NtStatus.Success;
			}

			// First see if it's a directory
			VirtualDirectoryNode dirNode = this.GetDirectoryNode(trimmedPath);
			if (dirNode != null)
			{
				info.IsDirectory = true;
				info.Context = dirNode;

				// For a read-only FS, we still allow opens for directories with any access;
				// we just never allow modifications elsewhere.
				return NtStatus.Success;
			}

			// Then see if it's a file
			VirtualFileNode fileNode = this.GetFileNode(trimmedPath);
			if (fileNode == null)
			{
				// No such entry
				// On a read-only filesystem, don't allow creation.
				if (mode == FileMode.Open || mode == FileMode.OpenOrCreate)
				{
					return NtStatus.ObjectNameNotFound;
				}

				return NtStatus.AccessDenied;
			}

			info.IsDirectory = false;
			info.Context = fileNode;

			// Deny actual create/truncate operations on a read-only FS
			if (mode == FileMode.Create ||
				mode == FileMode.CreateNew ||
				mode == FileMode.Truncate ||
				mode == FileMode.Append)
			{
				return NtStatus.AccessDenied;
			}

			// IMPORTANT:
			// Do NOT reject opens just because write access is requested.
			// Let the handle be created and simply fail WriteFile/SetEndOfFile/etc.
			return NtStatus.Success;
		}

		public NtStatus DeleteDirectory(ReadOnlyNativeMemory<char> fileNamePtr, ref DokanFileInfo info)
		{
			return NtStatus.Success;
		}

		public NtStatus DeleteFile(ReadOnlyNativeMemory<char> fileNamePtr, ref DokanFileInfo info)
		{
			return NtStatus.Success;
		}
		public NtStatus FindFiles(
			ReadOnlyNativeMemory<char> fileNamePtr,
			out IEnumerable<FindFileInformation> files,
			ref DokanFileInfo info)
		{
			String path = new String(fileNamePtr.Span);
			String trimmedPath = path.Trim('\\', '/');

			VirtualDirectoryNode directory = this.GetDirectoryNode(trimmedPath);
			if (directory == null)
			{
				files = Enumerable.Empty<FindFileInformation>();
				return NtStatus.ObjectNameNotFound;
			}

			var list = new List<FindFileInformation>(directory.Children.Count);

			foreach (KeyValuePair<String, VirtualNode> child in directory.Children)
			{
				String name = child.Key;

				if (child.Value is VirtualDirectoryNode)
				{
					list.Add(new FindFileInformation
					{
						FileName = name.AsMemory(),
						Attributes = FileAttributes.Directory | FileAttributes.ReadOnly,
						Length = 0
					});
				}
				else if (child.Value is VirtualFileNode fileNode)
				{
					list.Add(new FindFileInformation
					{
						FileName = name.AsMemory(),
						Attributes = FileAttributes.ReadOnly,
						Length = fileNode.Length ?? 0
					});
				}
			}

			files = list;
			return NtStatus.Success;
		}

		private VirtualDirectoryNode GetDirectoryNode(String path)
		{
			VirtualDirectoryNode current = (VirtualDirectoryNode)this._rootNode;

			if (String.IsNullOrWhiteSpace(path))
			{
				// Root directory
				return current;
			}

			String[] segments = path.Split(
				new[] { '\\', '/' },
				StringSplitOptions.RemoveEmptyEntries);

			foreach (String segment in segments)
			{
				if (!current.Children.TryGetValue(segment, out VirtualNode next))
				{
					// Path segment does not exist
					return null;
				}

				if (next is not VirtualDirectoryNode directory)
				{
					// Path points to a file, not a directory
					return null;
				}

				current = directory;
			}

			return current;
		}

		private VirtualFileNode GetFileNode(String path)
		{
			VirtualDirectoryNode current = (VirtualDirectoryNode)this._rootNode;

			if (String.IsNullOrWhiteSpace(path))
			{
				return null;
			}

			String[] segments = path.Split(
				new[] { '\\', '/' },
				StringSplitOptions.RemoveEmptyEntries);

			foreach (String segment in segments)
			{
				if (!current.Children.TryGetValue(segment, out VirtualNode next))
				{
					// Path segment does not exist
					return null;
				}

				if (next is not VirtualDirectoryNode directory)
				{
					// Path points to a file, not a directory
					return next as VirtualFileNode;
				}

				current = directory;
			}

			return null;
		}

		public NtStatus FindFilesWithPattern(ReadOnlyNativeMemory<char> fileNamePtr, ReadOnlyNativeMemory<char> searchPatternPtr, out IEnumerable<FindFileInformation> files, ref DokanFileInfo info)
		{
			// Convert the native memory to normal Strings
			var path = new String(fileNamePtr.Span);          // e.g. "\" or "\Folder\Sub"
			var pattern = new String(searchPatternPtr.Span);  // e.g. "*" or "*.txt"

			// Normalise root: Dokan usually passes "\" or "\\"
			var trimmedPath = path.Trim('\\', '/');

			// Resolve the directory node from your virtual tree
			var directory = this.GetDirectoryNode(trimmedPath);

			if (directory == null)
			{
				// No such directory
				files = Enumerable.Empty<FindFileInformation>();
				return NtStatus.ObjectNameNotFound;
			}

			if (String.IsNullOrEmpty(pattern))
			{
				pattern = "*";
			}

			var list = new List<FindFileInformation>(directory.Children.Count);

			foreach (KeyValuePair<String, VirtualNode> child in directory.Children)
			{
				var name = child.Key;

				// Match against the search pattern (supports * and ?)
				if (!System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(pattern, name, ignoreCase: true))
				{
					continue;
				}

				if (child.Value is VirtualDirectoryNode)
				{
					list.Add(new FindFileInformation
					{
						FileName = name.AsMemory(),
						Attributes = FileAttributes.Directory | FileAttributes.ReadOnly,
						Length = 0
					});
				}
				else if (child.Value is VirtualFileNode fileNode)
				{
					list.Add(new FindFileInformation
					{
						FileName = name.AsMemory(),
						Attributes = FileAttributes.ReadOnly,
						Length = fileNode.Length ?? 0,
						// CreationTime = fileNode.CreationTime, // If you have these
						// LastAccessTime = fileNode.LastAccessTime,
						// LastWriteTime = fileNode.LastWriteTime,
					});
				}
			}

			files = list;
			return NtStatus.Success;
		}

		public NtStatus FindStreams(ReadOnlyNativeMemory<char> fileNamePtr, out IEnumerable<FindFileInformation> streams, ref DokanFileInfo info)
		{
			streams = Array.Empty<FindFileInformation>();

			return NtStatus.Success;
		}

		public NtStatus FlushFileBuffers(ReadOnlyNativeMemory<char> fileNamePtr, ref DokanFileInfo info)
		{
			return NtStatus.Success;
		}

		public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, ref DokanFileInfo info)
		{
			totalNumberOfBytes = this.VolumeSize;
			freeBytesAvailable = totalNumberOfFreeBytes = 0;
			return NtStatus.Success;
		}

		public NtStatus GetFileInformation(
			ReadOnlyNativeMemory<char> fileNamePtr,
			out ByHandleFileInformation fileInfo,
			ref DokanFileInfo info)
		{
			fileInfo = new ByHandleFileInformation();

			String path = new String(fileNamePtr.Span);
			String trimmedPath = path.Trim('\\', '/');

			// Root dir
			if (String.IsNullOrEmpty(trimmedPath))
			{
				fileInfo.Attributes = FileAttributes.Directory | FileAttributes.ReadOnly;
				fileInfo.Length = 0;
				fileInfo.CreationTime = this.Timestamp;
				fileInfo.LastAccessTime = this.Timestamp;
				fileInfo.LastWriteTime = this.Timestamp;
				return NtStatus.Success;
			}

			// Directory?
			VirtualDirectoryNode dirNode = this.GetDirectoryNode(trimmedPath);
			if (dirNode != null)
			{
				fileInfo.Attributes = FileAttributes.Directory | FileAttributes.ReadOnly;
				fileInfo.Length = 0;
				fileInfo.CreationTime = this.Timestamp;
				fileInfo.LastAccessTime = this.Timestamp;
				fileInfo.LastWriteTime = this.Timestamp;
				return NtStatus.Success;
			}

			// File?
			VirtualFileNode fileNode = this.GetFileNode(trimmedPath);
			if (fileNode == null)
			{
				return NtStatus.ObjectNameNotFound;
			}

			if (!fileNode.Length.HasValue)
			{
				var content = this.GetCachedContent(fileNode);
				fileNode.Length = content.Length;
			}

			fileInfo.Length = fileNode.Length.Value;
			fileInfo.Attributes = FileAttributes.ReadOnly | FileAttributes.Archive;

			fileInfo.CreationTime = this.Timestamp;
			fileInfo.LastAccessTime = this.Timestamp;
			fileInfo.LastWriteTime = this.Timestamp;

			return NtStatus.Success;
		}

		public NtStatus GetFileSecurity(ReadOnlyNativeMemory<char> fileNamePtr, out FileSystemSecurity security, AccessControlSections sections, ref DokanFileInfo info)
		{
			var everyoneSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
			
			if (info.IsDirectory)
			{
				var rule = new FileSystemAccessRule(
					everyoneSid,
					FileSystemRights.Read,
					InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
					PropagationFlags.None,
					AccessControlType.Allow);

				security = new DirectorySecurity();

				security.AddAccessRule(rule);
			}
			else
			{
				var rule = new FileSystemAccessRule(
					everyoneSid,
					FileSystemRights.Read,
					InheritanceFlags.None,
					PropagationFlags.None,
					AccessControlType.Allow);

				security = new FileSecurity();
				security.AddAccessRule(rule);
			}


			return NtStatus.NotImplemented;
		}

		public NtStatus GetVolumeInformation(NativeMemory<char> volumeLabel, out FileSystemFeatures features, NativeMemory<char> fileSystemName, out uint maximumComponentLength, ref uint volumeSerialNumber, ref DokanFileInfo info)
		{
			NativeMemoryHelper.SetString(volumeLabel, this.VolumeLabel);
	        NativeMemoryHelper.SetString(fileSystemName, this.FileSystemName);

			features = FileSystemFeatures.ReadOnlyVolume;
			maximumComponentLength = 1024;

			return NtStatus.Success;
		}

		public NtStatus LockFile(ReadOnlyNativeMemory<char> fileNamePtr, long offset, long length, ref DokanFileInfo info)
		{
			return NtStatus.NotImplemented;
		}

		public NtStatus Mounted(ReadOnlyNativeMemory<char> mountPoint, ref DokanFileInfo info)
		{
			return NtStatus.Success;
		}

		public NtStatus MoveFile(ReadOnlyNativeMemory<char> oldNamePtr, ReadOnlyNativeMemory<char> newNamePtr, bool replace, ref DokanFileInfo info)
		{
			return NtStatus.NotImplemented;
		}

		public NtStatus ReadFile(ReadOnlyNativeMemory<char> fileNamePtr, NativeMemory<byte> buffer, out int bytesRead, long offset, ref DokanFileInfo info)
		{
			String path = new String(fileNamePtr.Span);

			// Normalise root: Dokan usually passes "\" or "\\"
			String trimmedPath = path.Trim('\\', '/');

			var fileNode = this.GetFileNode(trimmedPath);

			if (fileNode == null)
			{
				bytesRead = 0;

				return NtStatus.Success;
			}

			var payload = this.GetCachedContent(fileNode);

			this.WritePayloadToBuffer(payload, buffer, offset, out bytesRead);

			return NtStatus.Success;
		}

		private Byte[] GetCachedContent(VirtualFileNode fileNode)
		{
			var content = MemoryCache.Default.Get(fileNode.Path) as Byte[];

			if (content != null) return content;

			content = fileNode.GetContent();

			MemoryCache.Default.Set(fileNode.Path, content, new CacheItemPolicy
			{
				SlidingExpiration = TimeSpan.FromSeconds(30)
			});

			return content ?? Array.Empty<Byte>();
		}

		private void WritePayloadToBuffer(
			Byte[] data,
			NativeMemory<Byte> buffer,
			Int64 offset,
			out Int32 bytesRead)
		{
			// Handle offsets beyond EOF
			if (offset >= data.Length || offset < 0)
			{
				bytesRead = 0;
				return;
			}

			// Compute how many bytes we can actually copy
			Int64 remaining = data.Length - offset;
			Int32 maxToCopy = buffer.Length;
			Int32 toCopy = (Int32)Math.Min(remaining, maxToCopy);

			// Copy slice [offset, offset + toCopy) to target buffer
			ReadOnlySpan<Byte> sourceSpan = new ReadOnlySpan<Byte>(data, (Int32)offset, toCopy);
			Span<Byte> targetSpan = buffer.Span;

			sourceSpan.CopyTo(targetSpan);

			bytesRead = toCopy;
		}

		public NtStatus SetAllocationSize(ReadOnlyNativeMemory<char> fileNamePtr, long length, ref DokanFileInfo info)
		{
			return NtStatus.NotImplemented;
		}

		public NtStatus SetEndOfFile(ReadOnlyNativeMemory<char> fileNamePtr, long length, ref DokanFileInfo info)
		{
			return NtStatus.NotImplemented;
		}

		public NtStatus SetFileAttributes(ReadOnlyNativeMemory<char> fileNamePtr, FileAttributes attributes, ref DokanFileInfo info)
		{
			return NtStatus.NotImplemented;
		}

		public NtStatus SetFileSecurity(ReadOnlyNativeMemory<char> fileNamePtr, FileSystemSecurity security, AccessControlSections sections, ref DokanFileInfo info)
		{
			return NtStatus.NotImplemented;
		}

		public NtStatus SetFileTime(ReadOnlyNativeMemory<char> fileNamePtr, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, ref DokanFileInfo info)
		{
			return NtStatus.NotImplemented;
		}

		public NtStatus UnlockFile(ReadOnlyNativeMemory<char> fileNamePtr, long offset, long length, ref DokanFileInfo info)
		{
			return NtStatus.NotImplemented;
		}

		public NtStatus Unmounted(ref DokanFileInfo info)
		{
			return NtStatus.Success;
		}

		public NtStatus WriteFile(ReadOnlyNativeMemory<char> fileNamePtr, ReadOnlyNativeMemory<byte> buffer, out int bytesWritten, long offset, ref DokanFileInfo info)
		{
			bytesWritten = 0;

			return NtStatus.NotImplemented;
		}

		public void ClearCache() => MemoryCache.Default.Trim(100);
	}
}
