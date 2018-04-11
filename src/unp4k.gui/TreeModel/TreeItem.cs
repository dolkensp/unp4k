using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media;

namespace unp4k.gui.TreeModel
{
	public abstract class TreeItem : INotifyCollectionChanged, INotifyPropertyChanged
	{
		public abstract String Title { get; }
		public virtual ImageSource Icon => null;
		public virtual String RelativePath => $"{this.Parent.RelativePath}\\{this.Title}".Trim('\\');
		public abstract TreeItem Parent { get; }
		public virtual Boolean Expanded { get; set; } = false;

		public virtual String SortKey => this.Title.ToLowerInvariant();
		public ZipObservableCollection Children { get; } = new ZipObservableCollection { };

		public event NotifyCollectionChangedEventHandler CollectionChanged;
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
