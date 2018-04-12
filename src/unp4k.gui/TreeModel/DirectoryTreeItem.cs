using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;

namespace unp4k.gui.TreeModel
{
	public class DirectoryTreeItem : TreeItem, IBranchItem
	{
		public virtual Boolean Expanded { get; set; } = false;

		public override String SortKey => $"__{this.Title.ToLowerInvariant()}";
		public override ImageSource Icon => IconManager.GetCachedFolderIcon(
			path: this.RelativePath, 
			iconSize: IconManager.IconSize.Large,
			folderType: IconManager.FolderType.Closed);

		public DirectoryTreeItem(String title, ITreeItem parent)
			: base(title, parent) { }
	}
}
