using ICSharpCode.TreeView;
using System.ComponentModel;
using System.Windows.Data;

namespace unp4k.gui.Extensions
{
	public static class SharpTreeViewExtensions
    {
		public static void ClearSort(this SharpTreeView treeView)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(treeView.ItemsSource);
			view.SortDescriptions.Clear();
		}

		public static void AddSort(this SharpTreeView treeView, SortDescription item)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(treeView.ItemsSource);
			view.SortDescriptions.Add(item);
		}
	}
}
