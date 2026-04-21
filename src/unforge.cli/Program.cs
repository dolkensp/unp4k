using System;
using System.IO;
using System.Linq;

namespace unforge.cli
{
	class Program
	{
		static void Main(params String[] args)
		{
			var ci = System.Globalization.CultureInfo.InvariantCulture;
			System.Threading.Thread.CurrentThread.CurrentCulture = ci;
			System.Threading.Thread.CurrentThread.CurrentUICulture = ci;

			// Parse command-line options
			var verbose = args.Any(a => a == "-v" || a == "--verbose");
			var logToFile = args.Any(a => a == "-l" || a == "--log");
			var inputArgs = args.Where(a => !a.StartsWith("-")).ToArray();

			// Configure logging
			DataForgeLogger.Instance.IsEnabled = true;
			DataForgeLogger.Instance.VerboseMode = verbose;

			if (inputArgs.Length == 0)
			{
				inputArgs = new String[] { "game.dcb" };
				// inputArgs = new String[] { "wrld.xml" };
				// inputArgs = new String[] { "Data" };
				// inputArgs = new String[] { @"S:\Mods\BuildXPLOR\archive-3.0\661655\Data\game.dcb" };
			}

			if (inputArgs.Length < 1 || inputArgs.Length > 1)
			{
				Console.WriteLine("Usage: unforge.exe [options] [infile]");
				Console.WriteLine();
				Console.WriteLine("Options:");
				Console.WriteLine("  -v, --verbose    Enable verbose output (show errors in console)");
				Console.WriteLine("  -l, --log        Write errors to a log file (infile.log)");
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

			var inputPath = inputArgs[0];

			if (Directory.Exists(inputPath))
			{
				foreach (var file in Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories))
				{
					if (new String[] { "ini", "txt" }.Contains(Path.GetExtension(file), StringComparer.InvariantCultureIgnoreCase)) continue;

					try
					{
						Console.WriteLine("Converting {0}", file.Replace(inputPath, ""));

						Smelter.Instance.Smelt(file);
					}
					catch (Exception ex)
					{
						DataForgeLogger.Instance.LogError(
							message: $"Failed to convert file",
							recordPath: file,
							exception: ex);
					}
				}
			}
			else
			{
				Smelter.Instance.Smelt(inputPath);
			}

			// Output summary
			Console.WriteLine();
			Console.WriteLine(DataForgeLogger.Instance.GetSummary());

			// Save log file if requested
			if (logToFile)
			{
				var logPath = Path.ChangeExtension(inputPath, "log");
				DataForgeLogger.Instance.SaveToFile(logPath);
				Console.WriteLine($"Log saved to: {logPath}");
			}
		}
	}

	public class Smelter
	{
		public static Smelter Instance { get; } = new Smelter { };

		private Boolean _overwrite;

		public void Smelt(String path, Boolean overwrite = true)
		{
			this._overwrite = overwrite;

			try
			{
				if (File.Exists(path))
				{
					if (Path.GetExtension(path) == ".dcb")
					{
						DataForgeLogger.Instance.LogInfo($"Processing DCB file: {path}");

						using var fileStream = File.OpenRead(path);
						var df = new DataForge(fileStream);
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

						var xml = CryXmlSerializer.ReadFile(path);

						if (xml != null)
						{
							xml.Save(Path.ChangeExtension(path, "xml"));
						}
						else
						{
							DataForgeLogger.Instance.LogDebug($"{path} already in XML format");
						}
					}
				}
			}
			catch (Exception ex)
			{
				DataForgeLogger.Instance.LogError(
					message: $"Error converting file",
					recordPath: path,
					exception: ex);

				Console.WriteLine("Error converting {0}: {1}", path, ex.Message);
			}
		}
	}
}
