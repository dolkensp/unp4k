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
		String RelativePath { get; }
		ITreeItem Parent { get; set; }

		String SortKey { get; }

		TreeItemObservableCollection Children { get; }
		IEnumerable<IStreamTreeItem> AllChildren { get; }
	}

	public abstract class TreeItem : ITreeItem
	{
		public virtual String Title { get; }
		public virtual ImageSource Icon => null;
		public virtual String RelativePath => $"{this.Parent.RelativePath}\\{this.Title}".Trim('\\');
		public virtual ITreeItem Parent { get; set; }

		public virtual String SortKey => this.Title.ToLowerInvariant();

		public virtual TreeItemObservableCollection Children { get; } = new TreeItemObservableCollection { };
		public virtual IEnumerable<IStreamTreeItem> AllChildren => this.Children.SelectMany(c => c.AllChildren).OfType<IStreamTreeItem>();
		
		public TreeItem(String title, ITreeItem parent)
		{
			this.Title = title;
			this.Parent = parent;
		}
	}
}
