using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using unp4k.gui.Extensions;
using unp4k.gui.TreeModel;
using Path = System.IO.Path;

namespace unp4k.gui
{
	//public class OpenFileCommand : ICommand
	//{
	//	public void Execute(Object parameter)
	//	{
	//		MessageBox.Show(@"""Hello, world!"" from "
	//			+ (parameter ?? "somewhere secret").ToString());
	//	}

	//	public Boolean CanExecute(Object parameter)
	//	{
	//		return true;
	//	}

	//	public event EventHandler CanExecuteChanged;
	//}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class ArchiveExplorer : Window
	{
		private Stream _pakFile;
		private ZipFile _pak;
		private TreeExtractor _extractor;
		private ZipFileTreeItem _root;

		public const Int32 FILTER_DELAY = 250;
		public const Int32 FILTER_PING = 50;

		public ArchiveExplorer()
		{
			InitializeComponent();

			this.Icon = IconManager.GetCachedFileIcon("data.zip", IconManager.IconSize.Large);

			trvFileExplorer.Focus();

			new Thread(async () =>
			{
				while (true)
				{
					await Task.Delay(FILTER_PING);

					while (this._lastFilterText != this._activeFilterText)
					{
						var now = this._lastFilterTime ?? DateTime.Now;

						while ((DateTime.Now - now).TotalMilliseconds < FILTER_DELAY)
						{
							await Task.Delay(FILTER_PING);
							now = this._lastFilterTime ?? DateTime.Now;
						}

						var filterText = this._lastFilterText;

						await Dispatcher.Invoke(async () =>
						{
							await this.NotifyNodesAsync(this._root);
						});

						this._activeFilterText = filterText;

						await Task.Delay(FILTER_PING);
					}
				}
			}).Start();
		}

		~ArchiveExplorer()
		{
			if (this._pak != null)
			{
				this._pak.Close();
				this._pak = null;
			}

			if (this._pakFile != null)
			{
				this._pakFile.Dispose();
				this._pakFile = null;
			}
		}

		public async Task OpenP4kAsync(String path)
		{
			TreeView treeView = this.trvFileExplorer;

			new Thread(() =>
			{
				var pakFile = File.OpenRead(path);
				var pak = new ZipFile(pakFile);

				var root = new ZipFileTreeItem(pak, null, Path.GetFileName(path));

				var filter = this._lastFilterText;

				if (filter.Equals("Filter...", StringComparison.InvariantCultureIgnoreCase)) filter = null;
				
				this.Dispatcher.Invoke(async () =>
				{
					treeView.Items.Clear();

					if (this._pak != null)
					{
						this._pak.Close();
						this._pak = null;
					}

					if (this._pakFile != null)
					{
						this._pakFile.Dispose();
						this._pakFile = null;
					}

					this._pak = pak;
					this._pakFile = pakFile;

					this._extractor = new TreeExtractor(pak, this.Filter);
					this._root = root;

					treeView.Items.Add(root);
				});
			}).Start();

			await Task.CompletedTask;
		}

		public Predicate<Object> Filter => (Object n) =>
		{
			var filter = this._lastFilterText;

			if (String.IsNullOrWhiteSpace(filter)) return true;

			if (n is DirectoryTreeItem directory)
			{
				return directory.Nodes.Any(z => z.Name.Contains(filter, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols));
			}

			if (n is ZipEntryTreeItem zipEntry)
			{
				return zipEntry.Entry.Name.Contains(filter, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols);
			}

			if (n is ZipFileTreeItem zipFile)
			{
				return zipFile.Nodes.Any(z => z.Name.Contains(filter, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols));
			}

			return false;
		};

		private void trvFileExplorer_Expanded(object sender, RoutedEventArgs e)
		{
			var node = e.OriginalSource as TreeViewItem;

			if (node != null)
			{
				node.Items.SortDescriptions.Clear();
				node.Items.SortDescriptions.Add(new SortDescription("SortKey", ListSortDirection.Ascending));

				node.Items.Filter = this.Filter;

				var treeItem = node.DataContext as TreeItem;

				if (treeItem != null)
				{
					treeItem.Expanded = true;
				}
			}
		}

		private async void mnuOpen_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var openFileDialog = new VistaOpenFileDialog
			{
				Filter = "Star Citizen Data Files|*.p4k",
				CheckFileExists = true,
				AddExtension = true,
				DefaultExt = ".p4k"
			};

			if (openFileDialog.ShowDialog() == true)
			{
				await this.OpenP4kAsync(openFileDialog.FileName);
			}
		}

		#region Mouse Support

		private async void trvFileExplorer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var treeView = sender as TreeView;

			var selectedItem = treeView.SelectedItem as TreeModel.TreeItem;

			new Thread(async () => await this._extractor.ExtractNodeAsync(selectedItem, false)).Start();
		}

		#endregion

		#region Keyboard Support

		private Dictionary<Key, Boolean> keyState = new Dictionary<Key, Boolean> { { Key.Enter, false } };

		private async void trvFileExplorer_KeyDown(object sender, KeyEventArgs e)
		{
			this.keyState[e.Key] = true;

			await Task.CompletedTask;
		}

		private async void trvFileExplorer_KeyUp(object sender, KeyEventArgs e)
		{
			if (this.keyState[Key.Enter])
			{
				var treeView = sender as TreeView;

				var selectedItem = treeView.SelectedItem as TreeModel.TreeItem;

				var useTemp = treeView.SelectedItem is ZipEntryTreeItem;

				new Thread(async () => await this._extractor.ExtractNodeAsync(selectedItem, useTemp)).Start();
			}

			this.keyState[e.Key] = false;
		}

		#endregion

		#region Inbound Drag and Drop Support

		private async void trvFileExplorer_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);

				var path = files.Where(f => Path.GetExtension(f).Equals(".p4k", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

				if (!String.IsNullOrWhiteSpace(path))
				{
					await this.OpenP4kAsync(path);
				}
			}
		}

		#endregion

		#region Filter Support

		private Thread _filterThread;
		private DateTime? _lastFilterTime;
		private String _lastFilterText = String.Empty;
		private String _activeFilterText = String.Empty;

		//private async Task FilterNodesAsync(ItemsControl node)
		//{
		//	node.Items.Filter = this.GetFilter();

		//	foreach (Object item in node.Items)
		//	{
		//		var child = node.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

		//		if (child == null) continue;

		//		await this.FilterNodesAsync(child);
		//	}
		//}

		private async Task NotifyNodesAsync(TreeItem node)
		{
			// Debug.WriteLine($"Touched {node.RelativePath}");

			node.Children.Touch();

			if (node.Expanded)
			{
				foreach (TreeItem item in node.Children)
				{
					await this.NotifyNodesAsync(item);
				}
			}
		}

		private async void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
		{
			var filter = txtFilter.Text;
			if (filter.Equals("Filter...", StringComparison.InvariantCultureIgnoreCase)) filter = String.Empty;

			if (filter == this._lastFilterText) return;

			this._lastFilterTime = DateTime.Now;
			this._lastFilterText = filter;
		}

		#endregion

		#region Placeholder Text Support

		private void txtFilter_GotFocus(object sender, RoutedEventArgs e)
		{
			if (txtFilter.Text.Equals("Filter...", StringComparison.InvariantCultureIgnoreCase))
			{
				txtFilter.Text = String.Empty;
			}
		}

		private void txtFilter_LostFocus(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrWhiteSpace(txtFilter.Text))
			{
				txtFilter.Text = "Filter...";
			}
		}

		#endregion

		#region Outbound Drag and Drop Support

		private Point _start;

		private void trvFileExplorer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			this._start = e.GetPosition(null);
		}

		private void trvFileExplorer_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed) return;
			if (this.trvFileExplorer.SelectedItem == null) return;

			Point mpos = e.GetPosition(null);
			Vector diff = this._start - mpos;

			if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance &&
				Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
			{
				// right about here you get the file urls of the selected items.
				// should be quite easy, if not, ask.
				String[] files = new String[] { };
				String dataFormat = DataFormats.FileDrop;
				DataObject dataObject = new DataObject(dataFormat, files);
				DragDrop.DoDragDrop(this.trvFileExplorer, dataObject, DragDropEffects.Move);
			}
		}

		#endregion
	}
}
