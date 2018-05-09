using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.TreeView;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;
using unforge;
using unp4k.gui.Plugins;

namespace unp4k.gui.TreeModel
{
	public class TreeItemFactory
	{
		public TreeItemFactory Instance { get; } = new TreeItemFactory { };

		
		public void Touch()
		{
			foreach (var item in this)
			{
				this.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Replace, item, item));
			}
		}
	}
}
