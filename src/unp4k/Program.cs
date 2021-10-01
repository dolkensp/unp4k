using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
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
            ZipFile pak = new ZipFile(pakFile);
            byte[] buf = new byte[4096];

            foreach (ZipEntry entry in pak)
            {
                try
                {
                    if (args[1].StartsWith("*.")) args[1] = args[1].Substring(1);                                                                                           // Enable *.ext format for extensions
                    if (args[1] == ".*" ||                                                                                                                                  // Searching for everything
                        args[1] == "*" ||                                                                                                                                   // Searching for everything
                        entry.Name.ToLowerInvariant().Contains(args[1].ToLowerInvariant()) ||                                                                               // Searching for keywords / extensions
                        (args[1].EndsWith("xml", StringComparison.InvariantCultureIgnoreCase) && entry.Name.EndsWith(".dcb", StringComparison.InvariantCultureIgnoreCase))) // Searching for XMLs - include game.dcb
                    {
                        FileInfo target = new FileInfo(entry.Name);
                        if (!target.Directory.Exists) target.Directory.Create();
                        if (!target.Exists)
                        {
                            Console.WriteLine($"{entry.CompressionMethod} | {entry.Name}");
                            using Stream s = pak.GetInputStream(entry);
                            using FileStream fs = File.Create(entry.Name);
                            StreamUtils.Copy(s, fs, buf);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception while extracting {entry.Name}: {ex.Message}");
                    /*
                     * TODO: This needs updating, not entirely sure how this works and it doesnt seem to work, for me anyway. A new experimental thing or something?
                     * Probably unwise to upload something like this to a server due to bandwidth costs/the abusability of it.
                     * 
                    try
                    {
                        using (var client = new HttpClient { })
                        {
                            // var server = "http://herald.holoxplor.local";
                            var server = "https://herald.holoxplor.space";

                            client.DefaultRequestHeaders.Add("client", "unp4k");

                            using (var content = new MultipartFormDataContent("UPLOAD----"))
                            {
                                content.Add(new StringContent($"{ex.Message}\r\n\r\n{ex.StackTrace}"), "exception", entry.Name);

                                using (var errorReport = client.PostAsync($"{server}/p4k/exception/{entry.Name}", content).Result)
                                {
                                    if (errorReport.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        Console.WriteLine("This exception has been reported.");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("There was a problem whilst attempting to report this error.");
                    }
                    */
                }
            }
        }
	}
}
