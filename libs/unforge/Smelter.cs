using System.IO;
using System.Xml;

namespace unforge
{
	public static class Smelter
	{
		public static void Smelt(FileInfo inFile, FileInfo outFile)
		{
			Logger.LogInfo("");
			if (inFile.Extension == ".dcb")
            {
				using BinaryReader br = new(inFile.Open(FileMode.Open, FileAccess.Read, FileShare.None));
				new DataForge(br, inFile.Length < 0x0e2e00).Save(Path.ChangeExtension(outFile.FullName, "xml"));
			}
			else
            {
				XmlDocument xml = CryXmlSerializer.ReadFile(inFile.FullName);
				if (xml != null) xml.Save(Path.ChangeExtension(outFile.FullName, "xml"));
				else Logger.LogInfo($"{outFile.FullName} already in XML format");
			}
		}
	}
}
