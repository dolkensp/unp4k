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
		
		public ITreeItem AddStream(Func<Stream> @delegate, String fullPath, ITreeItem parent, DateTime lastWriteTimeUtc)
		{
			var path = Path.GetDirectoryName(fullPath).Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

			if (path.Length > 0) parent = this.GetParentRelativePath(path, parent);

			if (parent == null) return null;

			IStreamTreeItem streamItem = new StreamTreeItem(Path.GetFileName(fullPath), parent, lastWriteTimeUtc, @delegate);

			foreach (var factory in factories)
			{
				streamItem = factory.Handle(streamItem);
			}

			parent.Children.Add(streamItem);

			return streamItem;
		}

		private ITreeItem GetParentRelativePath(String[] fullPath, ITreeItem parent = null)
		{
			if (fullPath.Length == 0) return parent;

			var key = fullPath[0];

			var directory = this
				.OfType<DirectoryTreeItem>()
				.Where(d => d.Title == key)
				.FirstOrDefault();

			if (directory == null)
			{
				directory = new DirectoryTreeItem(key, parent);

				this.Add(directory);
			}

			return directory.Children.GetParentRelativePath(fullPath.Skip(1).ToArray(), directory);
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
