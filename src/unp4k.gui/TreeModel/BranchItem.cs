using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace unp4k.gui.TreeModel
{
	public interface IBranchItem : ITreeItem
	{
		Boolean Expanded { get; set; }
	}

	public class BranchProxy : ITreeItem
	{
		public TreeItem Archive { get; }

		public String Title => String.Empty;

		public ImageSource Icon => this.Archive.Icon;

		public String RelativePath => this.Parent.RelativePath;

		public ITreeItem Parent { get; set; }

		public String SortKey => this.Archive.SortKey;

		public TreeItemObservableCollection Children => this.Archive.Children;

		public IEnumerable<IStreamTreeItem> AllChildren => this.Archive.AllChildren;

		public BranchProxy(TreeItem node)
		{
			this.Archive = node;
		}
	}
}
