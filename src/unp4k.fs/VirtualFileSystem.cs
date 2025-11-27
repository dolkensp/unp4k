using DokanNet;
using LTRData.Extensions.Native.Memory;
using System.IO.Enumeration;
using System.Runtime.Caching;
using System.Security.AccessControl;
using System.Text;
using unforge;

namespace unp4k.fs
{
	internal class VirtualNode
	{
		public required String Path { get; set; }
	}

	internal class VirtualFileNode : VirtualNode
	{
		public Int64? Length { get; set; }
	}

	internal class VirtualDirectoryNode : VirtualNode
	{
		public Dictionary<string, VirtualNode> Children { get; } = new Dictionary<string, VirtualNode>(StringComparer.OrdinalIgnoreCase);
	}

	internal class VirtualFileSystem : IDokanOperations2
	{
		public int DirectoryListingTimeoutResetIntervalMs => 30000;

		private DataForgeStream _dataForge;
		private VirtualNode _fileTree;

		public VirtualFileSystem(DataForgeStream dataForge)
		{
			this._dataForge = dataForge;
			this._fileTree = this.BuildFileTree(dataForge.RecordMap.Keys.ToArray());
		}

		private VirtualNode BuildFileTree(IEnumerable<String> paths)
		{
			var root = new VirtualDirectoryNode { Path = String.Empty };
			foreach (var path in paths)
			{
				var segments = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
				var currentNode = root;
				for (int i = 0; i < segments.Length; i++)
				{
					var segment = segments[i];
					if (i == segments.Length - 1)
					{
						// It's a file
						currentNode.Children[segment] = new VirtualFileNode
						{
							Path = path,
						};
					}
					else
					{
						// It's a directory
						if (!currentNode.Children.TryGetValue(segment, out var nextNode))
						{
							nextNode = new VirtualDirectoryNode
							{
								Path = path
							};
							currentNode.Children[segment] = nextNode;
						}
						currentNode = (VirtualDirectoryNode)nextNode;
					}
				}
			}
			return root;
		}

		public void Cleanup(ReadOnlyNativeMemory<char> fileNamePtr, ref DokanFileInfo info)
		{
		}

		public void CloseFile(ReadOnlyNativeMemory<char> fileNamePtr, ref DokanFileInfo info)
		{

		}

		public NtStatus CreateFile(ReadOnlyNativeMemory<char> fileNamePtr, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, ref DokanFileInfo info)
		{
			if (access.HasFlag(DokanNet.FileAccess.WriteData)) return NtStatus.AccessDenied;
			if (access.HasFlag(DokanNet.FileAccess.AppendData)) return NtStatus.AccessDenied;
			if (access.HasFlag(DokanNet.FileAccess.WriteAttributes)) return NtStatus.AccessDenied;
			if (access.HasFlag(DokanNet.FileAccess.WriteExtendedAttributes)) return NtStatus.AccessDenied;
			if (access.HasFlag(DokanNet.FileAccess.GenericWrite)) return NtStatus.AccessDenied;

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

		public NtStatus FindFiles(ReadOnlyNativeMemory<char> fileNamePtr, out IEnumerable<FindFileInformation> files, ref DokanFileInfo info)
		{
			files = Array.Empty<FindFileInformation>();

			return NtStatus.NotImplemented;
		}

		private VirtualDirectoryNode? GetDirectoryNode(String path)
		{
			VirtualDirectoryNode current = (VirtualDirectoryNode)this._fileTree;

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
				if (!current.Children.TryGetValue(segment, out VirtualNode? next))
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

		private VirtualFileNode? GetFileNode(String path)
		{
			VirtualDirectoryNode current = (VirtualDirectoryNode)this._fileTree;

			if (String.IsNullOrWhiteSpace(path))
			{
				return null;
			}

			String[] segments = path.Split(
				new[] { '\\', '/' },
				StringSplitOptions.RemoveEmptyEntries);

			foreach (String segment in segments)
			{
				if (!current.Children.TryGetValue(segment, out VirtualNode? next))
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
			String path = new String(fileNamePtr.Span);          // e.g. "\" or "\Folder\Sub"
			String pattern = new String(searchPatternPtr.Span);  // e.g. "*" or "*.txt"

			// Normalise root: Dokan usually passes "\" or "\\"
			String trimmedPath = path.Trim('\\', '/');

			// Resolve the directory node from your virtual tree
			VirtualDirectoryNode? directory = this.GetDirectoryNode(trimmedPath);

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
				String name = child.Key;

				// Match against the search pattern (supports * and ?)
				if (!FileSystemName.MatchesSimpleExpression(pattern, name, ignoreCase: true))
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
			totalNumberOfBytes = this._dataForge.Length; // .OuterXML.Length;
			freeBytesAvailable = totalNumberOfFreeBytes = 0;
			return NtStatus.Success;
		}

		public NtStatus GetFileInformation(ReadOnlyNativeMemory<char> fileNamePtr, out ByHandleFileInformation fileInfo, ref DokanFileInfo info)
		{
			fileInfo = new ByHandleFileInformation { };

			String path = new String(fileNamePtr.Span);

			// Normalise root: Dokan usually passes "\" or "\\"
			String trimmedPath = path.Trim('\\', '/');

			var fileNode = this.GetFileNode(trimmedPath);

			if (fileNode == null)
			{
				return NtStatus.ObjectNameNotFound;
			}

			if (fileNode.Length.HasValue) fileInfo.Length = fileNode.Length.Value;
			else
			{
				var xmlBlob = this.ReadXmlRecordAsBlob(fileNode);

				fileNode.Length = xmlBlob.Length;
				fileInfo.Length = xmlBlob.Length;
			}

			return NtStatus.Success;
		}

		public NtStatus GetFileSecurity(ReadOnlyNativeMemory<char> fileNamePtr, out FileSystemSecurity? security, AccessControlSections sections, ref DokanFileInfo info)
		{
			security = null;

			return NtStatus.Success;
		}

		public NtStatus GetVolumeInformation(NativeMemory<char> volumeLabel, out FileSystemFeatures features, NativeMemory<char> fileSystemName, out uint maximumComponentLength, ref uint volumeSerialNumber, ref DokanFileInfo info)
		{
			NativeMemoryHelper.SetString(volumeLabel, "Star Citizen");
	        NativeMemoryHelper.SetString(fileSystemName, $"DataForge {this._dataForge.FileVersion}");

			features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch | FileSystemFeatures.UnicodeOnDisk;
			maximumComponentLength = 255;

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
				return NtStatus.ObjectNameNotFound;
			}

			var xmlBlob = this.ReadXmlRecordAsBlob(fileNode);

			this.WritePayloadToBuffer(xmlBlob, buffer, offset, out bytesRead);

			return NtStatus.Success;
		}

		private String ReadXmlRecordAsBlob(VirtualFileNode fileNode)
		{

			var xmlBlob = MemoryCache.Default.Get(fileNode.Path) as String;

			if (String.IsNullOrWhiteSpace(xmlBlob))
			{
				var xmlElement = this._dataForge.ReadXmlRecord(this._dataForge.RecordMap[fileNode.Path]);

				xmlBlob = xmlElement.OuterXml;

				MemoryCache.Default.Set(fileNode.Path, xmlBlob, new CacheItemPolicy
				{
					SlidingExpiration = TimeSpan.FromSeconds(30)
				});
			}

			return xmlBlob;
		}

		private void WritePayloadToBuffer(String payload, NativeMemory<Byte> buffer, Int64 offset, out Int32 bytesRead)
		{
			// 1. Serialize the payload to a UTF-8 byte array
			Byte[] data = Encoding.UTF8.GetBytes(payload);

			// 2. Handle offsets beyond EOF
			Int64 totalLength = data.LongLength;
			if (offset >= totalLength || offset < 0)
			{
				bytesRead = 0;
				return;
			}

			// 3. Compute how many bytes we can actually copy
			Int64 remaining = totalLength - offset;
			Int32 maxToCopy = buffer.Length; // NativeMemory<T>.Length is Int32
			Int32 toCopy = (Int32)Math.Min(remaining, maxToCopy);

			// 4. Copy the slice [offset, offset + toCopy) into the provided buffer
			ReadOnlySpan<Byte> sourceSpan = new ReadOnlySpan<Byte>(data, (Int32)offset, toCopy);
			Span<Byte> targetSpan = buffer.Span;

			sourceSpan.CopyTo(targetSpan);

			bytesRead = toCopy;
		}

		//		/// <summary>
		//		/// Read from file using unmanaged buffers.
		//		/// </summary>
		//		public override NtStatus ReadFile(ReadOnlyNativeMemory<char> fileName, NativeMemory<byte> buffer, out int bytesRead, long offset, ref DokanFileInfo info)
		//		{
		//			if (info.Context is not FileStream stream) // memory mapped read
		//			{
		//				using (stream = new FileStream(GetPath(fileName), FileMode.Open, System.IO.FileAccess.Read))
		//				{
		//					DoRead(stream.SafeFileHandle, buffer.Address, buffer.Length, out bytesRead, offset);
		//				}
		//			}
		//			else // normal read
		//			{
		//#pragma warning disable CA2002
		//				lock (stream) //Protect from overlapped read
		//#pragma warning restore CA2002
		//				{
		//					DoRead(stream.SafeFileHandle, buffer.Address, buffer.Length, out bytesRead, offset);
		//				}
		//			}

		//			return Trace($"Unsafe{nameof(ReadFile)}", fileName, info, DokanResult.Success, "out " + bytesRead.ToString(),
		//				offset.ToString(CultureInfo.InvariantCulture));
		//		}

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
	}
}
