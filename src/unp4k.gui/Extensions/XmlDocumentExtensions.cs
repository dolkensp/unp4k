using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
