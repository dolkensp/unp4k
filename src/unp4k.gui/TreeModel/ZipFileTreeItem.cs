using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System.Globalization;
using System.Diagnostics;

namespace unp4k.gui.TreeModel
{
	public class ZipFileTreeItem : TreeItem
	{
		public ZipFile Archive { get; }

		public override TreeItem Parent { get; }
		public override String Title { get; }

		public override String RelativePath => String.Empty;
		public override ImageSource Icon => IconManager.GetCachedFileIcon(
			path: this.Title, 
			iconSize: IconManager.IconSize.Large);

		internal List<ZipEntry> Nodes { get; } = new List<ZipEntry> { };

		public ZipFileTreeItem(ZipFile zipFile, TreeItem parent, String name = null)
		{
			this.Archive = zipFile;
			this.Title = name;
			this.Parent = parent;

			foreach (ZipEntry entry in this.Archive)
			{
				this.Nodes.Add(entry);
				this.Children.AddEntry(entry, this);
			}

			if (String.IsNullOrWhiteSpace(this.Title)) this.Title = this.Archive.Name;
			if (String.IsNullOrWhiteSpace(this.Title)) this.Title = "Data.p4k";
		}

		private ZipFileTreeItem(IEnumerable<ZipEntry> nodes, ZipFile zipFile, TreeItem parent, String name)
		{
			this.Archive = zipFile;
			this.Title = name;
			this.Parent = parent;

			foreach (ZipEntry entry in nodes)
			{
				this.Nodes.Add(entry);
				this.Children.AddEntry(entry, this);
			}

			if (String.IsNullOrWhiteSpace(this.Title)) this.Title = this.Archive.Name;
			if (String.IsNullOrWhiteSpace(this.Title)) this.Title = "Data.p4k";
		}
	}
}
