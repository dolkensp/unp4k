using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using unforge;

namespace unp4k.fs
{
	internal class CompressedFileSystem : VirtualDirectoryNode
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

		public static VirtualNode BuildFileTree(ZipFile p4k)
		{
			var root = new CompressedFileSystem { Path = String.Empty };
			foreach (ZipEntry entry in p4k)
			{
				var segments = entry.Name.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
				var currentNode = (VirtualDirectoryNode)root;
				for (int i = 0; i < segments.Length; i++)
				{
					var segment = segments[i];
					if (i == segments.Length - 1)
					{
						if (System.IO.Path.GetExtension(segment).Equals(".dcb", StringComparison.InvariantCultureIgnoreCase))
						{
							var bufferedStream = new MemoryStream();
							using (var s = p4k.GetInputStream(entry))
							{
								s.CopyTo(bufferedStream);
						
								// It's a DataForge file
								var df = new DataForge(bufferedStream);
								var dfRoot = DataForgeFileSystem.BuildFileTree(df) as VirtualDirectoryNode;
								
								foreach (var child in dfRoot.Children)
								{
									currentNode.Children[child.Key] = child.Value;
								}
							}
						}

						// It's a file
						currentNode.Children[segment] = new VirtualFileNode
						{
							Path = entry.Name,
							Length = entry.Size,
							GetContent = () =>
							{
								using (var s = p4k.GetInputStream(entry))
								using (var ms = new MemoryStream())
								{
									s.CopyTo(ms);

									try
									{
										ms.Position = 0;
										var node = CryXmlSerializer.ReadStream(ms, ByteOrderEnum.AutoDetect, writeLog: false);

										using (var cryXmlStream = new MemoryStream())
										using (var writer = XmlWriter.Create(cryXmlStream, _xmlSettings))
										{
											node.WriteTo(writer);
											writer.Flush();

											return cryXmlStream.ToArray();
										}
									}
									catch
									{
										return ms.ToArray();
									}
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
