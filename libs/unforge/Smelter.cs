using System;
using System.IO;
using System.Xml;

namespace unforge
{
	public static class Smelter
	{
		private static bool _overwrite;
		public static void Smelt(string path, bool overwrite = true)
		{
			_overwrite = overwrite;
			try
			{
				if (File.Exists(path))
				{
					if (Path.GetExtension(path) == ".dcb")
					{
						using BinaryReader br = new(File.OpenRead(path));
						bool legacy = new FileInfo(path).Length < 0x0e2e00;
						DataForge df = new(br, legacy);
						df.Save(Path.ChangeExtension(path, "xml"));
					}
					else
					{
						if (!_overwrite)
						{
							if (!File.Exists(Path.ChangeExtension(path, "raw")))
							{
								File.Move(path, Path.ChangeExtension(path, "raw"));
								path = Path.ChangeExtension(path, "raw");
							}
						}
						XmlDocument xml = CryXmlSerializer.ReadFile(path);
						if (xml != null) xml.Save(Path.ChangeExtension(path, "xml"));
						else Logger.LogInfo($"{path} already in XML format");
					}
				}
			}
			catch (Exception e)
			{
				Logger.LogException(e);
			}
		}
	}
}
