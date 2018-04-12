using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System.Globalization;
using System.Diagnostics;
using unp4k.gui.Extensions;

namespace unp4k.gui.TreeModel
{
	public class ZipFileTreeItem : TreeItem, IBranchItem
	{
		public virtual ZipFile Archive { get; }

		public virtual Boolean Expanded { get; set; } = false;

		public override String RelativePath => String.Empty;
		public override ImageSource Icon => IconManager.GetCachedFileIcon(
			path: this.Title, 
			iconSize: IconManager.IconSize.Large);

		public ZipFileTreeItem(ZipFile zipFile, String name = null, ITreeItem parent = null)
			: base(zipFile.GetArchiveName(name), parent)
		{
			this.Archive = zipFile;

			foreach (ZipEntry entry in this.Archive)
			{
				this.Children.AddEntry(this.Archive, entry, this);
			}
		}
	}
}
