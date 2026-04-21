using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace unsocpak
{
	class Program
	{
		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

			// Parse command-line options
			var verbose = args.Any(a => a == "-v" || a == "--verbose");
			var logToFile = args.Any(a => a == "-l" || a == "--log");
			var overwrite = args.Any(a => a == "-o" || a == "--overwrite");
			var inputArgs = args.Where(a => !a.StartsWith("-")).ToArray();

			// Configure logging
			SocpakLogger.Instance.IsEnabled = true;
			SocpakLogger.Instance.VerboseMode = verbose;

			if (inputArgs.Length == 0)
			{
				PrintUsage();
				return;
			}

			var inputPath = inputArgs[0];

			// Collect all socpak files to process
			var socpakFiles = CollectSocpakFiles(inputPath);

			if (socpakFiles.Length == 0)
			{
				Console.WriteLine($"No .socpak files found matching: {inputPath}");
				return;
			}

			Console.WriteLine($"Found {socpakFiles.Length} .socpak file(s) to extract");
			SocpakLogger.Instance.LogInfo($"Found {socpakFiles.Length} socpak files");

			var counter = 0;

			foreach (var socpakFile in socpakFiles)
			{
				counter++;
				var percentComplete = (int)Math.Round((double)counter / socpakFiles.Length * 100);

				try
				{
					var outputDir = Path.GetDirectoryName(socpakFile);
					if (String.IsNullOrEmpty(outputDir))
						outputDir = Directory.GetCurrentDirectory();

					Console.Write($"\r[{percentComplete,3}%] Extracting: {Path.GetFileName(socpakFile)}".PadRight(80));

					var entriesExtracted = ExtractSocpak(socpakFile, outputDir, overwrite);

					SocpakLogger.Instance.LogSuccess(socpakFile, entriesExtracted);

					if (verbose)
					{
						Console.WriteLine();
						Console.WriteLine($"  -> Extracted {entriesExtracted} entries to {outputDir}");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine();
					Console.WriteLine($"Error extracting {socpakFile}: {ex.Message}");

					SocpakLogger.Instance.LogError(
						message: "Exception during extraction",
						socpakFile: socpakFile,
						exception: ex);
				}
			}

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine(SocpakLogger.Instance.GetSummary());

			// Save log file if requested
			if (logToFile)
			{
				var logPath = Path.Combine(
					Path.GetDirectoryName(socpakFiles[0]) ?? Directory.GetCurrentDirectory(),
					"unsocpak.log");
				SocpakLogger.Instance.SaveToFile(logPath);
				Console.WriteLine($"Log saved to: {logPath}");
			}
		}

		static String[] CollectSocpakFiles(String inputPath)
		{
			// Check if it's a specific file
			if (File.Exists(inputPath))
			{
				if (inputPath.EndsWith(".socpak", StringComparison.OrdinalIgnoreCase))
				{
					return new[] { Path.GetFullPath(inputPath) };
				}
				Console.WriteLine($"Warning: {inputPath} is not a .socpak file");
				return Array.Empty<String>();
			}

			// Check if it's a directory
			if (Directory.Exists(inputPath))
			{
				return Directory.GetFiles(inputPath, "*.socpak", SearchOption.AllDirectories)
					.Select(Path.GetFullPath)
					.ToArray();
			}

			// Check if it's a glob pattern
			var directory = Path.GetDirectoryName(inputPath);
			var pattern = Path.GetFileName(inputPath);

			if (String.IsNullOrEmpty(directory))
				directory = Directory.GetCurrentDirectory();

			if (!Directory.Exists(directory))
			{
				Console.WriteLine($"Directory not found: {directory}");
				return Array.Empty<String>();
			}

			// If pattern is *.socpak or similar, use it directly
			if (pattern.Contains("*") || pattern.Contains("?"))
			{
				return Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
					.Where(f => f.EndsWith(".socpak", StringComparison.OrdinalIgnoreCase))
					.Select(Path.GetFullPath)
					.ToArray();
			}

			// Otherwise treat as directory search
			return Directory.GetFiles(directory, "*.socpak", SearchOption.AllDirectories)
				.Select(Path.GetFullPath)
				.ToArray();
		}

		static Int32 ExtractSocpak(String socpakPath, String outputDir, Boolean overwrite)
		{
			var entriesExtracted = 0;
			var buffer = new byte[4096];

			using (var fs = File.OpenRead(socpakPath))
			using (var zipStream = new ZipInputStream(fs))
			{
				ZipEntry entry;
				while ((entry = zipStream.GetNextEntry()) != null)
				{
					try
					{
						// Skip directory entries (entries ending with / or with no name)
						if (entry.IsDirectory || String.IsNullOrEmpty(entry.Name))
							continue;

						var destinationPath = Path.Combine(outputDir, entry.Name);
						var destinationDir = Path.GetDirectoryName(destinationPath);

						// Ensure the directory exists
						if (!String.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
						{
							Directory.CreateDirectory(destinationDir);
						}

						// Check if file exists
						if (File.Exists(destinationPath))
						{
							if (overwrite)
							{
								File.Delete(destinationPath);
							}
							else
							{
								SocpakLogger.Instance.LogSkipped(socpakPath, $"File exists: {entry.Name}");
								continue;
							}
						}

						using (var outputStream = File.Create(destinationPath))
						{
							StreamUtils.Copy(zipStream, outputStream, buffer);
						}
						entriesExtracted++;
					}
					catch (Exception ex)
					{
						SocpakLogger.Instance.LogWarning(
							message: $"Failed to extract entry: {ex.Message} | Stack: {ex.StackTrace}",
							socpakFile: socpakPath,
							entryName: entry.Name);
					}
				}
			}

			return entriesExtracted;
		}

		static void PrintUsage()
		{
			Console.WriteLine("unsocpak - Star Citizen .socpak extractor");
			Console.WriteLine();
			Console.WriteLine("Usage: unsocpak.exe [options] <path>");
			Console.WriteLine();
			Console.WriteLine("Options:");
			Console.WriteLine("  -v, --verbose     Enable verbose output (show all operations)");
			Console.WriteLine("  -l, --log         Write extraction log to file (unsocpak.log)");
			Console.WriteLine("  -o, --overwrite   Overwrite existing files (default: skip)");
			Console.WriteLine();
			Console.WriteLine("Arguments:");
			Console.WriteLine("  <path>            Path to .socpak file, directory, or glob pattern");
			Console.WriteLine();
			Console.WriteLine("Examples:");
			Console.WriteLine("  unsocpak.exe file.socpak              Extract single file");
			Console.WriteLine("  unsocpak.exe *.socpak                 Extract all socpak files in current dir");
			Console.WriteLine("  unsocpak.exe /path/to/dir             Extract all socpak files recursively");
			Console.WriteLine("  unsocpak.exe -v -o /path/to/dir       Verbose with overwrite");
		}
	}
}
