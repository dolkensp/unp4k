using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using unp4k.gui.TreeModel;

namespace unp4k.gui.Plugins
{
	public interface IFormatFactory
	{
		IStreamTreeItem Handle(IStreamTreeItem node);
		IStreamTreeItem Extract(IStreamTreeItem node);
	}

	public class CryXmlFormatFactory : IFormatFactory
	{
		public IStreamTreeItem Handle(IStreamTreeItem node) { return node; }

		public IStreamTreeItem Extract(IStreamTreeItem node)
		{
			var supportedExtensions = new HashSet<String>(StringComparer.InvariantCultureIgnoreCase) { ".xml", ".mtl" };

			if (supportedExtensions.Contains(Path.GetExtension(node.RelativePath)))
			{
				using (var dataStream = node.Stream)
				{
					dataStream.Seek(0, SeekOrigin.Begin);

					using (var br = new BinaryReader(dataStream))
					{
						try
						{
							Debug.WriteLine($"Checking {node.Text}");

							var peek = br.PeekChar();

							// File is already XML
							if (peek == '<') return node;

							String header = br.ReadFString(7);

							if (header == "CryXml" || header == "CryXmlB" || header == "CRY3SDK")
							{
								dataStream.Seek(0, SeekOrigin.Begin);

								var xml = unforge.CryXmlSerializer.ReadStream(dataStream);

								return new CryXmlTreeItem(node, xml);
							}
						}
						catch (Exception)
						{ }
					}
				}
			}

			return node;
		}
	}

	public class DataForgeFormatFactory : IFormatFactory
	{
		public IStreamTreeItem Handle(IStreamTreeItem node)
		{
			if (Path.GetExtension(node.Title).Equals(".dcb", StringComparison.InvariantCultureIgnoreCase))
			{
				using (var dataStream = node.Stream)
				{
					dataStream.Seek(0, SeekOrigin.Begin);

					using (var br = new BinaryReader(dataStream))
					{
						try
						{
							var forge = new unforge.DataForge(br);

							return new DataForgeTreeItem(node, forge);
						}
						catch (Exception)
						{ }
					}
				}
			}

			return node;
		}

		public IStreamTreeItem Extract(IStreamTreeItem node) { return node; }
	}
}
