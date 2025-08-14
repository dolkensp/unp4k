using System;
using System.Linq;
using System.Windows.Media;

namespace unp4k.gui.TreeModel
{
	public class DirectoryTreeItem : TreeItem, IBranchItem
	{
		public virtual Boolean Expanded { get; set; } = false;

		public override DateTime LastModifiedUtc => this.AllChildren
			.OfType<IStreamTreeItem>()
			.Max(t => t.LastModifiedUtc);

		public override Int64 StreamLength => this.AllChildren
			.OfType<IStreamTreeItem>()
			.Sum(t => t.StreamLength);

		private String _sortKey;
		public override String SortKey =>
			this._sortKey = this._sortKey ??
			$"{this.ParentTreeItem?.SortKey}\\__{this.Text}".Trim('\\');

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
