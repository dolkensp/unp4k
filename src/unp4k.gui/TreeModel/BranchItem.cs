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

	public abstract class BranchItem : TreeItem, IBranchItem
	{
		public virtual Boolean Expanded { get; set; }

		public BranchItem(String title, ITreeItem parent) : base(title, parent) { }
	}
}
