using DokanNet;
using DokanNet.Logging;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using unforge;

namespace unp4k.fs
{
	internal class Program
	{
		static void Main(string[] args)
		{
			// Debugging convenience
			if (args.Length == 0) args = [@"D:\Roberts Space Industries\StarCitizen\PTU\Data.p4k", @"S:"];

			var filePath = args.FirstOrDefault();
			var cleanupWorkspaceDirectory = false;
			var workspaceDirectory = Path.GetFullPath(args.Skip(1).FirstOrDefault(Path.Combine(Path.ChangeExtension(filePath, "unp4k"))), Environment.CurrentDirectory);

			if (!File.Exists(filePath)) throw new FileNotFoundException($"Unable to find file at path {filePath}");

			try
			{
				Console.WriteLine($"Creating virtual filesystem from {filePath}...");

				var vfs = GetFileSystem(filePath);

				if (Path.GetFullPath(workspaceDirectory) == Path.GetPathRoot(workspaceDirectory))
				{
					if (DriveInfo.GetDrives().Where(d => d.Name == Path.GetPathRoot(workspaceDirectory)).Any())
						throw new InvalidOperationException("Refusing to mount to root of existing drive");
				}
				else if (Directory.Exists(workspaceDirectory))
				{
					try
					{
						if (Directory.GetFileSystemEntries(workspaceDirectory).Length > 0)
						{
							throw new Exception($"Refusing to mount to non-empty directory {workspaceDirectory}");
						}
					}
					catch (IOException)
					{
						cleanupWorkspaceDirectory = true;
						Directory.Delete(workspaceDirectory);
						Directory.CreateDirectory(workspaceDirectory);
					}
				}
				else
				{
					cleanupWorkspaceDirectory = true;
					Directory.CreateDirectory(workspaceDirectory);
				}

				if (String.IsNullOrWhiteSpace(workspaceDirectory)) throw new Exception("Unable to resolve workspace");

				Console.WriteLine($"File system created. Mounting filesystem to {workspaceDirectory}...");
				Console.WriteLine();

				using var mre = new ManualResetEvent(false);
				using var dokanLogger = new ConsoleLogger("[Dokan] ");
				using var dokan = new Dokan(dokanLogger);

				Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
				{
					e.Cancel = true;
					mre.Set();
				};

				var dokanBuilder = new DokanInstanceBuilder(dokan)
					.ConfigureOptions(options =>
					{
						options.Options = DokanOptions.WriteProtection; // DokanOptions.DebugMode | DokanOptions.StderrOutput;
						options.MountPoint = workspaceDirectory;
					});

				using (var dokanInstance = dokanBuilder.Build(vfs))
				{
					// TODO: Put a menu system here for unmounting, remounting, etc.

					var loop = true;

					do
					{
						var blank = new string(' ', Console.WindowWidth);
						Console.CursorVisible = false;

						Console.SetCursorPosition(0, 0);
						Console.WriteLine("unp4k Filesystem mounted. Press the corresponding key to adjust settings, or Q / ESC to quit:");
						Console.Write(blank);
						Console.WriteLine($"1: Adjust Max Reference Depth ({DataForge.MaxReferenceDepth})");
						Console.WriteLine($"2: Adjust Max Pointer Depth   ({DataForge.MaxPointerDepth})");
						Console.WriteLine($"3: Adjust Max Nodes           ({DataForge.MaxNodes})");
						Console.Write(blank);
						Console.WriteLine("Q / ESC: Quit");
						var position = Console.GetCursorPosition();
						Console.Write(blank);
						Console.Write(blank);
						Console.Write(blank);
						Console.Write(blank);
						Console.Write(blank);
						Console.Write(blank);
						Console.Write(blank);
						Console.SetCursorPosition(position.Left, position.Top);
						Console.CursorVisible = false;

						var key = Console.ReadKey();

						switch (key.Key)
						{
							case ConsoleKey.D1:
							case ConsoleKey.NumPad1:
								Console.WriteLine();
								Console.WriteLine("Max Reference Depth controls how deep nested references will be followed when reading data.");
								Console.WriteLine("Increasing this may result in longer read times and higher memory usage.");
								Console.WriteLine("The default is 1, but higher numbers may be useful if you often need to trace referenced data.");
								Console.WriteLine();
								Console.Write($"Enter new Max Reference Depth (Min: 1, Current: {DataForge.MaxReferenceDepth}, Max: 1000): ");
								if (Int32.TryParse(Console.ReadLine(), out var newRefDepth) && newRefDepth >= 1 && newRefDepth <= 1000)
								{
									DataForge.MaxReferenceDepth = newRefDepth;

									vfs.ClearCache();
								}
								break;
								case ConsoleKey.D2:
								case ConsoleKey.NumPad2:
								Console.WriteLine();
								Console.WriteLine("WARNING: Max Pointer Depth is a safety mechanism to control how deep recursive structures should go.");
								Console.WriteLine("         Increasing this should have no impact, but acts as a safety against data corruption.");
								Console.WriteLine();
								Console.Write($"Enter new Max Pointer Depth (Min: 10, Current: {DataForge.MaxPointerDepth}, Max: 1000):");
								if (Int32.TryParse(Console.ReadLine(), out var newPtrDepth) && newPtrDepth >= 10 && newPtrDepth <= 1000)
								{
									DataForge.MaxPointerDepth = newPtrDepth;

									vfs.ClearCache();
								}
								break;
								case ConsoleKey.D3:
								case ConsoleKey.NumPad3:
								Console.WriteLine();
								Console.WriteLine("WARNING: Max Nodes is a safety mechanism only, and should be large enough to fit all nodes in a file.");
								Console.WriteLine("         Only adjust this if you encounter errors in the XML output.");
								Console.WriteLine();
								Console.Write($"Enter new Max Nodes (Min: 1000, Current: {DataForge.MaxNodes}, Max: 1000000):");
								if (Int32.TryParse(Console.ReadLine(), out var newMaxNodes) && newMaxNodes >= 1000 && newMaxNodes <= 1000000)
								{
									DataForge.MaxNodes = newMaxNodes;

									vfs.ClearCache();
								}
								break;
							case ConsoleKey.Escape:
							case ConsoleKey.Q:
								loop = false;
								break;
						}
					} while (loop);
				}
			}
			finally
			{
				if (cleanupWorkspaceDirectory) Directory.Delete(workspaceDirectory);
			}
		}

		private static VirtualFileSystem GetFileSystem(string filePath)
		{
			if (Path.GetExtension(filePath).Equals(".p4k", StringComparison.OrdinalIgnoreCase))
			{
				var key = new Byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };

				var p4k = new ZipFile(filePath) { Key = key };

				var rootNode = CompressedFileSystem.BuildFileTree(p4k);

				return new VirtualFileSystem(rootNode)
				{
					VolumeLabel = "Star Citizen",
					FileSystemName = "P4K Compressed Archive",
					Timestamp = File.GetLastWriteTimeUtc(filePath),
					VolumeSize = new FileInfo(filePath).Length,
				};
			}

			if (Path.GetExtension(filePath).Equals(".dcb", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine($"Loading DataForge file from {filePath}...");

				var df = new DataForge(File.OpenRead(filePath));

				Console.WriteLine("DataForge file loaded. Creating virtual filesystem...");

				var rootNode = DataForgeFileSystem.BuildFileTree(df);

				return new VirtualFileSystem(rootNode)
				{
					VolumeLabel = "Star Citizen",
					FileSystemName = $"DataForge {df.FileVersion}",
					Timestamp = File.GetLastWriteTimeUtc(filePath),
					VolumeSize = df.Length,
				};
			}

			throw new NotImplementedException("Unsupported file type for mounting");
		}
	}
}
