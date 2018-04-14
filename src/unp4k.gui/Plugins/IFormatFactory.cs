using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using unp4k.gui.Plugins;
using unp4k.gui.TreeModel;

namespace unp4k.gui.TreeModel
{
	public interface IStreamTreeItem : ITreeItem
	{
		Stream Stream { get; }
	}

	public class StreamTreeItem : TreeItem, IStreamTreeItem
	{
		public Stream Stream => this._streamDelegate();

		public override ImageSource Icon => IconManager.GetCachedFileIcon(
			path: this.Title,
			iconSize: IconManager.IconSize.Large);

		private Func<Stream> _streamDelegate;

		//private Func<Stream> GetSeekableDelegate(Stream stream)
		//{
		//	return () =>
		//	{
		//		var targetStream = this._stream ?? stream;

		//		if (targetStream?.CanSeek == false)
		//		{
		//			this._stream = new MemoryStream { };
		//			targetStream.CopyTo(this._stream);
		//			targetStream.Dispose();
		//			targetStream = this._stream;
		//		}

		//		this._stream = targetStream;

		//		this._stream.Seek(0, SeekOrigin.Begin);

		//		return this._stream;
		//	};
		//}
		
		// public StreamTreeItem(String title, ITreeItem parent, Stream stream)
		// 	: base(title, parent)
		// {
		// 	this._streamDelegate = this.GetSeekableDelegate(stream);
		// }

		public StreamTreeItem(String title, ITreeItem parent, Func<Stream> @delegate)
			: base(title, parent)
		{
			this._streamDelegate = () =>
			{
				var targetStream = @delegate();

				if (targetStream?.CanSeek == true) return targetStream;

				var buffer = new MemoryStream { };

				targetStream.CopyTo(buffer);
				targetStream.Dispose();

				return buffer;
			};
		}
	}
}

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
							Debug.WriteLine($"Checking {node.Title}");

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

							return new DataForgeTreeItem(node.Title, node.Parent, forge);
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
