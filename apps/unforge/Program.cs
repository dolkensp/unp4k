using System.Globalization;
using unforge;

Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

if (args.Length == 0)
{
	Logger.Log("################################################################################\n");
	Logger.Log("                             unforge <> Star Citizen                             ");
	Logger.Log(
		"\nConverts any Star Citizen binary file into an actual XML file.\n" +
		"CryXml files (.xml) are saved as .raw in the original location.\n" +
		"DataForge files (.dcb) are saved as .xml in the original location.\n\n" +
		"Can also convert all compatible files in a directory, and it's sub-directories. In that case, all CryXml files are saved in-place, " +
		"and any DataForge files are saved to both .xml and extracted to the original component locations."
		);
	Logger.NewLine();
	Logger.Log(@"Windows PowerShell: .\unforge " + '"' + "[optional-InFilePath]" + '"');
	Logger.Log(@"Windows Command Prompt: unforge " + '"' + "[optional-InFilePath]" + '"');
	Logger.Log(@"Linux Terminal: ./unforge " + '"' + "[optional-InFilePath]" + '"');
	Logger.Log("\n################################################################################");
	Logger.Log("\nPress any key to exit.");
	Console.ReadKey();
	return;
}
else if ((args.Length > 0) && Directory.Exists(args[0]))
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