using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Net.Http;

namespace unp4k
{
	class Program
	{
		static void Main(string[] args)
		{
			var key = new Byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };

			if (args.Length == 0) args = new[] { @"Data.p4k" };

			if (args.Length == 1) args = new[] { args[0], "*.*" };

			using (var pakFile = File.OpenRead(args[0]))
			{
				var pak = new ZipFile(pakFile) { Key = key };
				byte[] buf = new byte[4096];

				foreach (ZipEntry entry in pak)
				{
					try
					{
						var crypto = entry.IsAesCrypted ? "Crypt" : "Plain";

						if (args[1].StartsWith("*.")) args[1] = args[1].Substring(1);                                                                                           // Enable *.ext format for extensions

						if (args[1] == ".*" ||                                                                                                                                 // Searching for everything
							args[1] == "*" ||                                                                                                                                   // Searching for everything
							entry.Name.ToLowerInvariant().Contains(args[1].ToLowerInvariant()) ||                                                                               // Searching for keywords / extensions
							(args[1].EndsWith("xml", StringComparison.InvariantCultureIgnoreCase) && entry.Name.EndsWith(".dcb", StringComparison.InvariantCultureIgnoreCase))) // Searching for XMLs - include game.dcb
						{
							var target = new FileInfo(entry.Name);

							if (!target.Directory.Exists) target.Directory.Create();

							if (!target.Exists)
							{
								Console.WriteLine($"{entry.CompressionMethod} | {crypto} | {entry.Name}");

								using (Stream s = pak.GetInputStream(entry))
								{
									using (FileStream fs = File.Create(entry.Name))
									{
										StreamUtils.Copy(s, fs, buf);
									}
								}

								// target.Delete();
							}
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Exception while extracting {entry.Name}: {ex.Message}");

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
					}
				}
			}
		}
	}
}
