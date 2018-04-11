using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Windows.Media;
using Path = System.IO.Path;

namespace unp4k.gui.TreeModel
{
	public class ZipEntryTreeItem : TreeItem
	{
		public override TreeItem Parent { get; }
		public ZipEntry Entry { get; }

		public override String Title => Path.GetFileName(this.Entry.Name);
		public override ImageSource Icon => IconManager.GetCachedFileIcon(
			path: this.Entry.Name,
			iconSize: IconManager.IconSize.Large);

		public ZipEntryTreeItem(ZipEntry entry, TreeItem parent)
		{
			this.Entry = entry;
			this.Parent = parent;
		}
	}
}
