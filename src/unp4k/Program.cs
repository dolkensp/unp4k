using ICSharpCode.SharpZipLib.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace unp4k
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0) args = new[] { @"Data.p4k" };

			if (args.Length == 1) args = new[] { args[0], "*.*" };

			using (var pakFile = File.OpenRead(args[0]))
			{
				var pak = new ZipFile(pakFile);
				
				foreach (ZipEntry entry in pak)
				{
					if (!entry.IsAesCrypted) continue;

					var crypto = entry.IsAesCrypted ? "Crypt" : "Plain";

					if (args[1] == "*.*" ||                                                                                                                                 // Searching for everything
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
								byte[] buf = new byte[4096];

								using (FileStream fs = File.Create(entry.Name))
								{
									StreamUtils.Copy(s, fs, buf);
								}
							}
						}
					}
				}
			}
		}
	}
}
