using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Linq;

namespace unp4k
{
	class Program
	{
		static void Main(string[] args)
		{
			var key = new Byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };

			// Parse command-line options
			var verbose = args.Any(a => a == "-v" || a == "--verbose");
			var logToFile = args.Any(a => a == "-l" || a == "--log");
			var inputArgs = args.Where(a => !a.StartsWith("-")).ToArray();

			// Configure logging
			P4kLogger.Instance.IsEnabled = true;
			P4kLogger.Instance.VerboseMode = verbose;

			if (inputArgs.Length == 0) inputArgs = new[] { @"Data.p4k" };

			if (inputArgs.Length == 1) inputArgs = new[] { inputArgs[0], "*.*" };

			var p4kPath = inputArgs[0];
			var filter = inputArgs[1];

			if (!File.Exists(p4kPath))
			{
				Console.WriteLine($"Error: File not found: {p4kPath}");
				Console.WriteLine();
				PrintUsage();
				return;
			}

			P4kLogger.Instance.LogInfo($"Opening P4K archive: {p4kPath}");
			P4kLogger.Instance.LogInfo($"Filter: {filter}");

			using (var pakFile = File.OpenRead(p4kPath))
			{
				var pak = new ZipFile(pakFile) { Key = key };
				byte[] buf = new byte[4096];

				P4kLogger.Instance.LogInfo($"Archive contains {pak.Count} entries");

				var totalEntries = 0;

				foreach (ZipEntry entry in pak)
				{
					totalEntries++;
					try
					{
						var crypto = entry.IsAesCrypted ? "Crypt" : "Plain";
						var normalizedFilter = filter;

						if (normalizedFilter.StartsWith("*.")) normalizedFilter = normalizedFilter.Substring(1);  // Enable *.ext format for extensions

						var shouldExtract = normalizedFilter == ".*" ||                                                                                              // Searching for everything
							normalizedFilter == "*" ||                                                                                                               // Searching for everything
							entry.Name.ToLowerInvariant().Contains(normalizedFilter.ToLowerInvariant()) ||                                                           // Searching for keywords / extensions
							(normalizedFilter.EndsWith("xml", StringComparison.InvariantCultureIgnoreCase) && entry.Name.EndsWith(".dcb", StringComparison.InvariantCultureIgnoreCase)); // Searching for XMLs - include game.dcb

						if (shouldExtract)
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

								// Verify extraction
								var extractedFile = new FileInfo(entry.Name);
								if (extractedFile.Exists)
								{
									if (extractedFile.Length != entry.Size && entry.Size > 0)
									{
										P4kLogger.Instance.LogWarning(
											message: $"Size mismatch: expected {entry.Size}, got {extractedFile.Length}",
											entryName: entry.Name,
											compressionMethod: entry.CompressionMethod.ToString());
									}

									P4kLogger.Instance.LogSuccess(
										entryName: entry.Name,
										compressionMethod: entry.CompressionMethod.ToString(),
										compressedSize: entry.CompressedSize,
										uncompressedSize: entry.Size,
										isEncrypted: entry.IsAesCrypted);
								}
								else
								{
									P4kLogger.Instance.LogError(
										message: "File was not created after extraction",
										entryName: entry.Name,
										compressionMethod: entry.CompressionMethod.ToString());
								}
							}
							else
							{
								P4kLogger.Instance.LogSkipped(entry.Name, "File already exists");
							}
						}
						else
						{
							P4kLogger.Instance.LogSkipped(entry.Name, "Does not match filter");
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Exception while extracting {entry.Name}: {ex.Message}");

						P4kLogger.Instance.LogError(
							message: "Exception during extraction",
							entryName: entry.Name,
							compressionMethod: entry.CompressionMethod.ToString(),
							compressedSize: entry.CompressedSize,
							uncompressedSize: entry.Size,
							exception: ex);
					}
				}

				P4kLogger.Instance.LogInfo($"Processed {totalEntries} entries");
			}

			// Output summary
			Console.WriteLine();
			Console.WriteLine(P4kLogger.Instance.GetSummary());

			// Save log file if requested
			if (logToFile)
			{
				var logPath = Path.ChangeExtension(p4kPath, "log");
				P4kLogger.Instance.SaveToFile(logPath);
				Console.WriteLine($"Log saved to: {logPath}");
			}
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage: unp4k.exe [options] <p4k-file> [filter]");
			Console.WriteLine();
			Console.WriteLine("Options:");
			Console.WriteLine("  -v, --verbose     Enable verbose output (show all operations)");
			Console.WriteLine("  -l, --log         Write extraction log to file (p4k-file.log)");
			Console.WriteLine();
			Console.WriteLine("Arguments:");
			Console.WriteLine("  <p4k-file>        Path to the .p4k archive file");
			Console.WriteLine("  [filter]          Optional filter (default: *.* for all files)");
			Console.WriteLine("                    Examples: *.xml, *.dcb, Data/*, weapon");
			Console.WriteLine();
			Console.WriteLine("Examples:");
			Console.WriteLine("  unp4k.exe Data.p4k                    Extract all files");
			Console.WriteLine("  unp4k.exe Data.p4k *.xml              Extract XML files (includes .dcb)");
			Console.WriteLine("  unp4k.exe -l Data.p4k *.xml           Extract with logging");
			Console.WriteLine("  unp4k.exe -v -l Data.p4k              Verbose extraction with log");
		}
	}
}
