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
				var path = @"d:\game.dcb";
				var legacy = new FileInfo(path).Length < 0x0e2e00;

				using var br = new BinaryReader(File.OpenRead(path));
				using var mre = new System.Threading.ManualResetEvent(false);
				using var dokanLogger = new ConsoleLogger("[Dokan] ");
				using var dokan = new Dokan(dokanLogger);

				Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
				{
					e.Cancel = true;
					mre.Set();
				};

				var df = new DataForgeStream(br, legacy);
				// br.BaseStream.Seek(0, SeekOrigin.Begin);
				// var df2 = new DataForge(br, legacy);
				var fs = new VirtualFileSystem(df);
				var dokanBuilder = new DokanInstanceBuilder(dokan)
					.ConfigureOptions(options =>
					{
						options.Options = DokanOptions.DebugMode | DokanOptions.StderrOutput;
						options.MountPoint = "s:\\";
					});
				using (var dokanInstance = dokanBuilder.Build(fs))
				{
					mre.WaitOne();
				}

				Console.WriteLine(@"Success");
			}
			catch (DokanException ex)
			{
				Console.WriteLine(@"Error: " + ex.Message);
			}

			Console.WriteLine("Hello, World!");

			Console.ReadKey();
		}
	}
}
