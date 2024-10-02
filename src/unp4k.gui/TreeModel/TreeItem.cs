using System;
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.TreeView;
using System.IO;
using unp4k.gui.Plugins;

namespace unp4k.gui.TreeModel
{
	public interface ITreeItem
	{
		String Title { get; }
		String SortKey { get; }
		String RelativePath { get; }

		DateTime LastModifiedUtc { get; }
		Int64 StreamLength { get; }

		ITreeItem ParentTreeItem { get; }
		IEnumerable<ITreeItem> AllChildren { get; }
		
		SharpTreeNodeCollection Children { get; }
		SharpTreeNode Parent { get; }
		Object Text { get; }
		Object Icon { get; }
		Object ToolTip { get; }
		Int32 Level { get; }
		Boolean IsRoot { get; }
		Boolean IsHidden { get; set; }
		Boolean IsVisible { get; }
		Boolean IsSelected { get; set; }
	}

	public interface IBranchItem : ITreeItem { }

	public abstract class TreeItem : SharpTreeNode, ITreeItem
	{
		public virtual String Title { get; }
		public ITreeItem ParentTreeItem => this.Parent as ITreeItem;
		
		private String _sortKey;
		public virtual String SortKey => 
			this._sortKey = this._sortKey ??
			$"{this.ParentTreeItem?.SortKey}\\{this.Text}".Trim('\\');

		private String _relativePath;
		public virtual String RelativePath =>
			this._relativePath = this._relativePath ?? 
			$"{this.ParentTreeItem?.RelativePath}\\{this.Text}".Trim('\\');

		private IEnumerable<ITreeItem> _allChildren;
		public virtual IEnumerable<ITreeItem> AllChildren =>
			this._allChildren = this._allChildren ??
			this.Children
				.OfType<ITreeItem>()
				.SelectMany(c => c.AllChildren.Union(new[] { c }))
				.OfType<ITreeItem>()
				.ToArray();

		public override Object Text => this.Title;

		public abstract DateTime LastModifiedUtc { get; }
		public abstract Int64 StreamLength { get; }

		internal TreeItem(String title)
		{
			this.Title = title;
		}

		// TODO: Factory selection
		private IFormatFactory[] factories = new IFormatFactory[] {
			new DataForgeFormatFactory { },
			new CryXmlFormatFactory { }
		};

		public ITreeItem AddStream(String fullPath, Func<Stream> @delegate, DateTime lastModifiedUtc, Int64 streamLength)
		{
			var path = Path.GetDirectoryName(fullPath).Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

			var parent = this.GetParentRelativePath(path);

			if (parent == null) return null;

			var streamItem = new StreamTreeItem(Path.GetFileName(fullPath), @delegate, lastModifiedUtc, streamLength);

			foreach (var factory in factories)
			{
				streamItem = factory.Handle(streamItem) as StreamTreeItem;
			}

			parent.Children.Add(streamItem);

			return streamItem;
		}

		internal ITreeItem GetParentRelativePath(String[] fullPath)
		{
			if (fullPath.Length == 0) return this;

			var key = fullPath[0];

			var directory = this
				.Children
				.OfType<DirectoryTreeItem>()
				.Where(d => d.Title == key)
				.FirstOrDefault();

			if (directory == null)
			{
				directory = new DirectoryTreeItem(key);

				this.Children.Add(directory);
			}

			return directory.GetParentRelativePath(fullPath.Skip(1).ToArray());
		}

		public void Sort()
		{
			if (this.Children.Count > 1)
			{
				this.Children.Sort((x1, x2) =>
				{
					if (x1 is TreeItem t1)
					{
						if (x2 is TreeItem t2)
						{
							return String.Compare(t1.SortKey, t2.SortKey, StringComparison.InvariantCultureIgnoreCase);
						}
					}

					return String.Compare($"{x1.Text}", $"{x2.Text}", StringComparison.InvariantCultureIgnoreCase);
				});
			}

			foreach (var child in this.Children.OfType<TreeItem>())
			{
				child.Sort();
			}
		}
	}
}
