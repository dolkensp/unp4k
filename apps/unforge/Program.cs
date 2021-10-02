using System.Globalization;
using System.Xml;
using unforge;

Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

if (args.Length == 0) args = new string[] { "game.v4.dcb" };
if (args.Length < 1 || args.Length > 1)
{
	Logger.LogInfo("Usage: unforge.exe [infile]");
	Logger.NewLine();
	Logger.LogInfo("Converts any Star Citizen binary file into an actual XML file.");
	Logger.LogInfo("CryXml files (.xml) are saved as .raw in the original location.");
	Logger.LogInfo("DataForge files (.dcb) are saved as .xml in the original location.");
	Logger.NewLine();
	Logger.LogInfo("Can also convert all compatible files in a directory, and it's");
	Logger.LogInfo("sub-directories. In that case, all CryXml files are saved in-place,");
	Logger.LogInfo("and any DataForge files are saved to both .xml and extracted to");
	Logger.LogInfo("the original component locations.");
	return;
}

if ((args.Length > 0) && Directory.Exists(args[0]))
{
	foreach (string file in Directory.GetFiles(args[0], "*.*", SearchOption.AllDirectories))
	{
		if (new string[] { "ini", "txt" }.Contains(Path.GetExtension(file), StringComparer.InvariantCultureIgnoreCase)) continue;
		try
		{
			Logger.LogInfo($"Converting {file.Replace(args[0], "")}");
			Smelter.Smelt(file);
		}
		catch (Exception e) 
		{
			Logger.LogException(e);
		}
	}
}
else Smelter.Smelt(args[0]);

static class Smelter
{
	private static bool _overwrite;
	internal static void Smelt(string path, bool overwrite = true)
	{
		_overwrite = overwrite;
		try
		{
			if (File.Exists(path))
			{
				if (Path.GetExtension(path) == ".dcb")
				{
					using BinaryReader br = new BinaryReader(File.OpenRead(path));
					bool legacy = new FileInfo(path).Length < 0x0e2e00;
					DataForge df = new DataForge(br, legacy);
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