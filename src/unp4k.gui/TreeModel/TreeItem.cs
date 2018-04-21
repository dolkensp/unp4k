using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media;

namespace unp4k.gui.TreeModel
{
	public interface ITreeItem
	{
		String Title { get; }
		ImageSource Icon { get; }
		String SortKey { get; }

		String RelativePath { get; }

		ITreeItem Parent { get; }
		TreeItemObservableCollection Children { get; }
		IEnumerable<IStreamTreeItem> AllChildren { get; }
	}

	public abstract class TreeItem : ITreeItem
	{
		public virtual String Title { get; }

		public virtual ImageSource Icon => null;
		
		private String _sortKey;
		public virtual String SortKey =>
			this._sortKey = this._sortKey ?? 
			this.Title.ToLowerInvariant();

		private String _relativePath;
		public virtual String RelativePath =>
			this._relativePath = this._relativePath ?? 
			$"{this.Parent.RelativePath}\\{this.Title}".Trim('\\');

		public virtual ITreeItem Parent { get; }

		public virtual TreeItemObservableCollection Children { get; } = new TreeItemObservableCollection { };

		private IStreamTreeItem[] _allChildren;
		public virtual IEnumerable<IStreamTreeItem> AllChildren =>
			this._allChildren = this._allChildren ??
			this.Children
				.SelectMany(c => c.AllChildren.OfType<ITreeItem>().Union(new[] { c }))
				.OfType<IStreamTreeItem>()
				.ToArray();

		public TreeItem(String title, ITreeItem parent)
		{
			this.Title = title;
			this.Parent = parent;
		}
	}
}
