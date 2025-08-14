using System.IO;
using System.Xml;

namespace unp4k.gui.Extensions
{
	public static class XmlDocumentExtensions
	{
		public static Stream GetStream(this XmlDocument xml)
		{
			var outStream = new MemoryStream { };
			xml.Save(outStream);
			return outStream;
		}
	}
}
