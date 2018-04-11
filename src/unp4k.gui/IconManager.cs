using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace unp4k.gui
{
	/// <summary>
	/// Internals are mostly from here: http://www.codeproject.com/Articles/2532/Obtaining-and-managing-file-and-folder-icons-using
	/// Caches all results.
	/// </summary>
	public static class IconManager
	{
		public enum FolderType : UInt32
		{
			Closed = Shell32.SHGFI_CLOSEDICON,
			Open = Shell32.SHGFI_OPENICON,
		}

		public enum IconSize : UInt32
		{
			/// <summary>
			/// Specify large icon - 32 pixels by 32 pixels.
			/// </summary>
			Large = Shell32.SHGFI_LARGEICON,
			/// <summary>
			/// Specify small icon - 16 pixels by 16 pixels.
			/// </summary>
			Small = Shell32.SHGFI_SMALLICON,
		}

		private static readonly Dictionary<String, ImageSource> _smallIconCache = new Dictionary<String, ImageSource>(StringComparer.InvariantCultureIgnoreCase);
		private static readonly Dictionary<String, ImageSource> _largeIconCache = new Dictionary<String, ImageSource>(StringComparer.InvariantCultureIgnoreCase);

		public static ImageSource GetCachedFileIcon(String path, IconSize iconSize, Boolean linkOverlay = false)
		{
			var cache = iconSize == IconSize.Large ? _largeIconCache : _smallIconCache;

			ImageSource icon;

			var cacheKey = Path.GetExtension(path);

			if (cacheKey == null) return null;

			if (cache.TryGetValue(cacheKey, out icon)) return icon;

			icon = IconManager.GetFileIcon(path, iconSize, linkOverlay).ToImageSource();

			cache.Add(cacheKey, icon);

			return icon;
		}

		public static ImageSource GetCachedFolderIcon(String path, IconSize iconSize, FolderType folderType)
		{
			var cache = iconSize == IconSize.Large ? _largeIconCache : _smallIconCache;

			ImageSource icon;

			var cacheKey = $"{path}:{folderType}";

			if (cache.TryGetValue(cacheKey, out icon)) return icon;

			icon = IconManager.GetFolderIcon(path, iconSize, folderType).ToImageSource();

			cache.Add(cacheKey, icon);

			return icon;
		}
		
		/// <summary>
		/// Returns an icon for a given file - indicated by the name parameter.
		/// </summary>
		/// <param name="path">Pathname for file.</param>
		/// <param name="iconSize">Large or small</param>
		/// <param name="linkOverlay">Whether to include the link icon</param>
		/// <returns>System.Drawing.Icon</returns>
		public static Icon GetFileIcon(String path, IconSize iconSize, Boolean linkOverlay = false)
		{
			var flags = Shell32.SHGFI_ICON | Shell32.SHGFI_USEFILEATTRIBUTES | (UInt32)iconSize;

			if (linkOverlay) flags += Shell32.SHGFI_LINKOVERLAY;

			var shfi = new Shell32.Shfileinfo { };

			var res = Shell32.SHGetFileInfo(
				path,
				Shell32.FILEATTRIBUTE_NORMAL,
				ref shfi,
				(UInt32)Marshal.SizeOf(shfi),
				flags);

			if (Object.Equals(res, IntPtr.Zero)) throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());

			try
			{
				return (Icon)Icon.FromHandle(shfi.hIcon).Clone();
			}
			catch
			{
				throw;
			}
			finally
			{
				User32.DestroyIcon(shfi.hIcon);
			}
		}

		public static Icon GetFolderIcon(String path, IconSize iconSize, FolderType folderType)
			{
				var flags = Shell32.SHGFI_ICON | Shell32.SHGFI_USEFILEATTRIBUTES | (UInt32)iconSize | (UInt32)folderType;

				var shfi = new Shell32.Shfileinfo { };

				var res = Shell32.SHGetFileInfo(
					path,
					Shell32.FILEATTRIBUTE_DIRECTORY,
					ref shfi,
					(UInt32)Marshal.SizeOf(shfi),
					flags);

				if (Object.Equals(res, IntPtr.Zero)) throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());

				try
				{
					return (Icon)Icon.FromHandle(shfi.hIcon).Clone();
				}
				catch
				{
					throw;
				}
				finally
				{
					User32.DestroyIcon(shfi.hIcon);
				}
			}

		static ImageSource ToImageSource(this Icon icon)
		{
			var imageSource = Imaging.CreateBitmapSourceFromHIcon(
				icon.Handle,
				Int32Rect.Empty,
				BitmapSizeOptions.FromEmptyOptions());

			return imageSource;
		}

		/// <summary>
		/// Wraps necessary Shell32.dll structures and functions required to retrieve Icon Handles using SHGetFileInfo. Code
		/// courtesy of MSDN Cold Rooster Consulting case study.
		/// </summary>
		static class Shell32
		{
			[StructLayout(LayoutKind.Sequential)]
			public struct Shfileinfo
			{
				public readonly IntPtr hIcon;

				private readonly Int32 iIcon;

				private readonly UInt32 dwAttributes;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
				private readonly String szDisplayName;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
				private readonly String szTypeName;
			};

			public const UInt32 SHGFI_ICON = 0x000000100;
			public const UInt32 SHGFI_LINKOVERLAY = 0x000008000;
			public const UInt32 SHGFI_LARGEICON = 0x000000000;
			public const UInt32 SHGFI_SMALLICON = 0x000000001;
			public const UInt32 SHGFI_CLOSEDICON = 0x000000000;
			public const UInt32 SHGFI_OPENICON = 0x000000002;
			public const UInt32 SHGFI_USEFILEATTRIBUTES = 0x000000010;
			public const UInt32 FILEATTRIBUTE_NORMAL = 0x00000080;
			public const UInt32 FILEATTRIBUTE_DIRECTORY = 0x00000010;
			public const UInt32 FILEATTRIBUTE_FILE = 0x00000100;

			[DllImport("Shell32.dll")]
			public static extern IntPtr SHGetFileInfo(
				String pszPath,
				UInt32 dwFileAttributes,
				ref Shfileinfo psfi,
				UInt32 cbFileInfo,
				UInt32 uFlags
				);
		}

		static class User32
		{
			[DllImport("User32.dll")]
			public static extern Int32 DestroyIcon(IntPtr hIcon);
		}
	}
}
