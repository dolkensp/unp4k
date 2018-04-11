using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace unp4k.gui.TreeModel
{
	public class DirectoryTreeItem : TreeItem
	{
		public override TreeItem Parent { get; }
		public override String Title { get; }

		public override String SortKey => $"__{this.Title.ToLowerInvariant()}";
		public override ImageSource Icon => IconManager.GetCachedFolderIcon(
			path: this.RelativePath, 
			iconSize: IconManager.IconSize.Large,
			folderType: IconManager.FolderType.Closed);

		internal List<ZipEntry> Nodes { get; } = new List<ZipEntry> { };

		public DirectoryTreeItem(String name, TreeItem parent)
		{
			this.Title = name;
			this.Parent = parent;
		}
	}
}
