using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace unp4k.gui.TreeModel
{
	public class ZipObservableCollection : ObservableCollection<TreeItem>
	{
		public void AddEntry(ZipEntry entry, TreeItem parent = null)
		{
			this.AddEntry(entry, 0, parent);
		}

		private void AddEntry(ZipEntry entry, Int32 startIndex, TreeItem parent = null)
		{
			var path = entry.Name.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).Skip(startIndex).ToArray();

			var key = path[0];

			// file - add the original entry
			if (path.Length == 1)
			{
				this.Add(new ZipEntryTreeItem(entry, parent));
			}

			// directory - add a directory entry
			if (path.Length > 1)
			{
				var directory = this
					.OfType<DirectoryTreeItem>()
					.Where(d => d.Title == key)
					.FirstOrDefault();

				if (directory == null)
				{
					directory = new DirectoryTreeItem(key, parent);

					this.Add(directory);
				}

				directory.Nodes.Add(entry);
				directory.Children.AddEntry(entry, startIndex + 1, directory);
			}
		}

		public void Touch()
		{
			foreach (var item in this)
			{
				this.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Replace, item, item));
			}
		}
	}
}
