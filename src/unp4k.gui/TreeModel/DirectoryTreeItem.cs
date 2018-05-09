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

		private String _sortKey;
		public override String SortKey =>
			this._sortKey = this._sortKey ?? 
			$"__{this.Title.ToLowerInvariant()}";

		private ImageSource _icon;
		public override Object Icon =>
			this._icon = this._icon ?? 
			IconManager.GetCachedFolderIcon(
				path: this.RelativePath, 
				iconSize: IconManager.IconSize.Large,
				folderType: IconManager.FolderType.Closed);

		public DirectoryTreeItem(String title)
			: base(title) { }
	}
}
