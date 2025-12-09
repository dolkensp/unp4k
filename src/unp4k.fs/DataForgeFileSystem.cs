using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using unforge;

namespace unp4k.fs
{
	internal class DataForgeFileSystem : VirtualDirectoryNode
	{
		private static XmlWriterSettings _xmlSettings = new XmlWriterSettings
		{
			OmitXmlDeclaration = true,
			Encoding = new UTF8Encoding(false), // UTF-8, no BOM
			Indent = true,
			IndentChars = "  ",
			NewLineChars = "\n",
			NewLineHandling = NewLineHandling.Replace,
			ConformanceLevel = ConformanceLevel.Document,
			CheckCharacters = false
		};

		public static VirtualNode BuildFileTree(DataForge dataForge)
		{
			var root = new DataForgeFileSystem { Path = String.Empty };
			var paths = dataForge.PathToRecordMap.Keys;

			foreach (var path in paths)
			{
				var segments = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
				var currentNode = (VirtualDirectoryNode)root;

				for (int i = 0; i < segments.Length; i++)
				{
					var segment = segments[i];
					if (i == segments.Length - 1)
					{
						// It's a file
						currentNode.Children[segment] = new VirtualFileNode
						{
							Path = path,

							GetContent = () =>
							{
								var node = dataForge.ReadRecordByPathAsXml(path);

								using (var ms = new MemoryStream())
								using (var writer = XmlWriter.Create(ms, _xmlSettings))
								{
									node.WriteTo(writer);
									writer.Flush();

									return ms.ToArray();
								}
							}
						};
					}
					else
					{
						// It's a directory
						if (!currentNode.Children.TryGetValue(segment, out var nextNode))
						{
							nextNode = new VirtualDirectoryNode
							{
								Path = String.Join('/', segments.Take(i + 1))
							};
							currentNode.Children[segment] = nextNode;
						}
						currentNode = (VirtualDirectoryNode)nextNode;
					}
				}
			}
			return root;
		}
	}
}
