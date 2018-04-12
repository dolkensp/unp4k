using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;
using unforge;
using unp4k.gui.Plugins;

namespace unp4k.gui.TreeModel
{
	public class TreeItemObservableCollection : ObservableCollection<ITreeItem>
	{
		// TODO: Factory selection
		private IFormatFactory[] factories = new IFormatFactory[] {
			new DataForgeFormatFactory { },
			new CryXmlFormatFactory { }
		};

		public void AddEntry(ZipFile archive, ZipEntry entry, ITreeItem parent = null)
		{
			IStreamTreeItem treeItem = new StreamTreeItem(Path.GetFileName(entry.Name), parent, () => archive.GetInputStream(entry));

			this.AddEntry(treeItem, entry.Name.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries), 0, parent);
		}

		public void AddEntry(IStreamTreeItem treeItem, String fullPath, ITreeItem parent = null)
		{
			this.AddEntry(treeItem, fullPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries), 0, parent);
		}

		private void AddEntry(IStreamTreeItem treeItem, String[] fullPath, Int32 startIndex, ITreeItem parent = null)
		{
			var path = fullPath.Skip(startIndex).ToArray();

			var key = path[0];

			// file - add the original entry
			if (path.Length == 1)
			{
				foreach (var factory in factories)
				{
					treeItem = factory.Handle(treeItem);
				}

				treeItem.Parent = parent;

				this.Add(treeItem);
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

				// directory.AllChildren.Add(treeItem);
				directory.Children.AddEntry(treeItem, fullPath, startIndex + 1, directory);
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
