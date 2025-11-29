using DokanNet;
using DokanNet.Logging;
using System;
using System.IO;
using unforge;

namespace unp4k.fs
{
	internal class Program
	{
		static void Main(string[] args)
		{
			try
			{
				if (args.Length == 0)
				{
					args = new String[] { @"D:\Projects\unp4k\.data\game.v7.dcb" };
				}

				var path = args[0];

				if (!File.Exists(path)) throw new FileNotFoundException($"Unable to find file at path {path}");

				var workspace = Path.Combine(Path.ChangeExtension(path, "forge"));
				if (String.IsNullOrWhiteSpace(workspace)) throw new Exception("Unable to resolve workspace");
				// if (Directory.Exists(workspace)) throw new Exception($"Unable to create workspace at {workspace}. Remove this folder, or rename your dcb file and try again.");

				Directory.CreateDirectory(workspace);

				var legacy = new FileInfo(path).Length < 0x0e2e00;

				using var mre = new ManualResetEvent(false);
				using var dokanLogger = new ConsoleLogger("[Dokan] ");
				using var dokan = new Dokan(dokanLogger);

				Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
				{
					e.Cancel = true;
					mre.Set();
				};

				Console.WriteLine($"Loading DataForge file from {path}...");

				var df = new DataForge(File.OpenRead(path), legacy);
				
				Console.WriteLine("DataForge file loaded. Creating virtual filesystem...");

				var fs = new VirtualFileSystem(df, DateTime.MinValue);

				Console.WriteLine($"File system created. Mounting filesystem to {workspace}...");

				var dokanBuilder = new DokanInstanceBuilder(dokan)
					.ConfigureOptions(options =>
					{
						// options.SingleThread = true;
						// options.Options = DokanOptions.DebugMode | DokanOptions.StderrOutput;
						options.MountPoint = workspace;
					});

				Console.WriteLine("Press ctrl+c to close.");

				using (var dokanInstance = dokanBuilder.Build(fs))
				{
					mre.WaitOne();
				}

				Directory.Delete(workspace);
			}
			catch (DokanException ex)
			{
				Console.WriteLine(@"Error: " + ex.Message);
			}
		}
	}
}
