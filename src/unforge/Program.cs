using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Xml;

namespace unforge
{
    internal static class Program
    {
		internal static void Main(params string[] args)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length == 0)
            {
                args = new string[] { "game.v4.dcb" };
                // args = new string[] { "wrld.xml" };
                // args = new string[] { "Data" };
            }

            if (args.Length < 1 || args.Length > 1)
			{
				Console.WriteLine("Usage: unforge.exe [infile]");
				Console.WriteLine();
				Console.WriteLine("Converts any Star Citizen binary file into an actual XML file.");
				Console.WriteLine("CryXml files (.xml) are saved as .raw in the original location.");
				Console.WriteLine("DataForge files (.dcb) are saved as .xml in the original location.");
				Console.WriteLine();
				Console.WriteLine("Can also convert all compatible files in a directory, and it's");
				Console.WriteLine("sub-directories. In that case, all CryXml files are saved in-place,");
				Console.WriteLine("and any DataForge files are saved to both .xml and extracted to");
				Console.WriteLine("the original component locations.");
				return;
			}

			if ((args.Length > 0) && Directory.Exists(args[0]))
            {
                foreach (var file in Directory.GetFiles(args[0], "*.*", SearchOption.AllDirectories))
                {
                    if (new string[] { "ini", "txt" }.Contains(Path.GetExtension(file), StringComparer.InvariantCultureIgnoreCase)) continue;
                    try
                    {
                        Console.WriteLine("Converting {0}", file.Replace(args[0], ""));
                        Smelter.Instance.Smelt(file);
                    }
                    catch (Exception) { }
                }
            }
            else Smelter.Instance.Smelt(args[0]);
        }
    }

	public class Smelter
	{
		public static Smelter Instance => new();
		private bool _overwrite;

		public void Smelt(string path, bool overwrite = true)
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
						else Console.WriteLine("{0} already in XML format", path);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error converting {0}: {1}", path, ex.Message);
			}
		}
	}
}
