using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;
using unp4k.gui.Plugins;
using unp4k.gui.Extensions;
using System.Windows.Media;

namespace unp4k.gui.TreeModel
{
	public class DataForgeTreeItem : StreamTreeItem, IStreamTreeItem, IBranchItem
	{
		public override IEnumerable<IStreamTreeItem> AllChildren => this.Children.SelectMany(c => c.AllChildren).OfType<IStreamTreeItem>().With(this);

		private unforge.DataForge DataForge { get; }

		public Boolean Expanded { get; set; }

		public DataForgeTreeItem(String title, ITreeItem parent, unforge.DataForge dataForge)
			: base(title, parent, () => dataForge.GetStream())
		{
			this.DataForge = dataForge;

			foreach ((String FileName, XmlDocument XmlDocument) entry in this.DataForge)
			{
				IStreamTreeItem treeItem = new StreamTreeItem(Path.GetFileName(entry.FileName), new BranchProxy(this), () =>
				{
					var outStream = new MemoryStream { };
					entry.XmlDocument.Save(outStream);
					return outStream;
				});

				// this.AllChildren.Add(treeItem);
				this.Children.AddEntry(treeItem, entry.FileName, this);
			}
		}
	}

	public class CryXmlTreeItem : StreamTreeItem, IStreamTreeItem
	{
		public CryXmlTreeItem(IStreamTreeItem node, XmlDocument xml)
			: base(node.Title, node.Parent, () => { var outStream = new MemoryStream { }; xml.Save(outStream); return outStream; })
		{
			// TODO: Extract contents into this.Children here
		}
	}
}
