using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace unp4k
{
	internal static class Program
	{
		internal static void Main(string[] args)
		{
			if (args.Length == 0) args = new[] { @"Data.p4k" };
			if (args.Length == 1) args = new[] { args[0], "*.*" };

            using FileStream pakFile = File.OpenRead(args[0]);
            ZipFile pak = new(pakFile)
            {
                UseZip64 = UseZip64.Dynamic
            };
            pak.KeysRequired += (object sender, KeysRequiredEventArgs e) => e.Key = new byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };

            foreach (ZipEntry entry in pak)
            {
                try
                {
                    if (args[1].StartsWith("*.")) args[1] = args[1][1..];                                                                                                   // Enable *.ext format for extensions
                    if (args[1] == ".*" ||                                                                                                                                  // Searching for everything
                        args[1] == "*" ||                                                                                                                                   // Searching for everything
                        entry.Name.ToLowerInvariant().Contains(args[1].ToLowerInvariant()) ||                                                                               // Searching for keywords / extensions
                        (args[1].EndsWith("xml", StringComparison.InvariantCultureIgnoreCase) && entry.Name.EndsWith(".dcb", StringComparison.InvariantCultureIgnoreCase))) // Searching for XMLs - include game.dcb
                    {
                        FileInfo target = new(entry.Name);
                        if (!target.Directory.Exists) target.Directory.Create();
                        if (!target.Exists)
                        {
                            Console.WriteLine($"Extracting > {entry.Name}");
                            using ZipInputStream s = new(pak.GetInputStream(entry));
                            using FileStream fs = File.Create(entry.Name);
                            s.CopyTo(fs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Exception while extracting {entry.Name}: {ex.Message}");
                    Console.WriteLine();
                }
            }
        }
	}
}
