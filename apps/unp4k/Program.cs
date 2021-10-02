using System.Reflection;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

if (args.Length == 0) args = new[] { "Data.p4k", "*.*" };
else if (args.Length == 1) args = new[] { args[0], "*.*" };
else if (args.Length == 2) args = new[] { args[0], args[1], "*.*" };

if (!File.Exists(args[0]) && !Directory.Exists(args[0]))
{
    Logger.LogError("Input path '" + args[0] + "' does not exist!");
    Console.ReadKey();
    return;
}
if (args.Length == 3)
{
    if (!File.Exists(args[1]) && !Directory.Exists(args[1]))
    {
        Logger.LogError("Output path '" + args[1] + "' does not exist!");
        Console.ReadKey();
        return;
    }
    if (Directory.GetFiles(args[1]).Length > 0)
    {
        Logger.LogError("Output path '" + args[1] + "' must be empty!");
        Console.ReadKey();
        return;
    }
}

using FileStream pakFile = File.OpenRead(args[0]);
ZipFile pak = new(pakFile)
{
    UseZip64 = UseZip64.Dynamic
};
pak.KeysRequired += (object sender, KeysRequiredEventArgs e) => e.Key = new byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };
byte[] buf = new byte[4096];

foreach (ZipEntry entry in pak)
{
    string filter = args[^1];
    string? outputPath = args.Length == 3 ? args[1] : null;
    if (filter.StartsWith("*.")) filter = filter[1..];                                                                                                     // Enable *.ext format for extensions
    if (filter == ".*" ||                                                                                                                                  // Searching for everything
        filter == "*" ||                                                                                                                                   // Searching for everything
        entry.Name.ToLowerInvariant().Contains(filter.ToLowerInvariant()) ||                                                                               // Searching for keywords / extensions
        (filter.EndsWith("xml", StringComparison.InvariantCultureIgnoreCase) && entry.Name.EndsWith(".dcb", StringComparison.InvariantCultureIgnoreCase))) // Searching for XMLs - include game.dcb
    {
        FileInfo target = new(entry.Name);
        if (target.Directory is not null)
        {
            if (!target.Directory.Exists) new DirectoryInfo(Path.Join(outputPath is not null ? outputPath : "star_citizen_extraction",
                target.Directory.FullName.Replace(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), string.Empty))).Create();
            if (!target.Exists)
            {
                using FileStream fs = File.Create(Path.Join(outputPath is not null ? outputPath : "star_citizen_extraction", entry.Name));
                try
                {
                    Logger.LogInfo($"[{entry.CompressionMethod}] Extracting > {entry.Name}");
                    using Stream s = pak.GetInputStream(entry);
                    StreamUtils.Copy(s, fs, buf);
                }
                catch (Exception e)
                {
                    Logger.LogException(e);
                }
            }
        }
        else throw new DirectoryNotFoundException($"A directory in '{target.FullName}' was not found.");
    }
}