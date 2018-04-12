using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Windows.Media;
using Path = System.IO.Path;

namespace unp4k.gui.TreeModel
{
	public class ZipEntryTreeItem : TreeItem
	{
		public ZipEntry Entry { get; }

		public override String Title => Path.GetFileName(this.Entry.Name);
		public override ImageSource Icon => IconManager.GetCachedFileIcon(
			path: this.Entry.Name,
			iconSize: IconManager.IconSize.Large);

		public ZipEntryTreeItem(ZipEntry entry, ITreeItem parent)
			: base(Path.GetFileName(entry.Name), parent)
		{
			this.Entry = entry;
		}
	}
}
