using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

using unforge;
using unlib;

namespace unp4k;
internal class Worker
{
    private static ConcurrentQueue<ZipEntry> filteredEntries = new();

    private static ZipFile pak;
    private static readonly byte[] decomBuffer = new byte[4096];
    private static int isDecompressableCount = 0;
    private static int isLockedCount = 0;
    private static long bytesSize = 0L;
    private static int fileErrors = 0;
    private static bool additionalFiles = false;

    internal static void ProcessGameData()
    {
        Console.Title = $"unp4k: Working on {Globals.P4kFile.FullName}";

        // Setup the stream from the Data.p4k and contain it as an ICSC ZipFile with the appropriate keys then enqueue all zip entries.
        Logger.LogInfo($"Processing Data.p4k before extraction, this may take a moment...");
        bool loadingTrigger = true;
        Task.Run(async () => 
        {
            Console.Write("Processing...");
            while (loadingTrigger)
            {
                Console.Write('.');
                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }
        });
        pak = new(Globals.P4kFile.Open(FileMode.Open, FileAccess.Read, FileShare.None));// The Data.p4k must be locked while it is being read to avoid corruption.
        pak.UseZip64 = UseZip64.On;
        pak.KeysRequired += (object sender, KeysRequiredEventArgs e) => e.Key = new byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };
        foreach (ZipEntry entry in pak) filteredEntries.Enqueue(entry);

        // Filter out zip entries which cannot be decompressed and/or are locked behind a cypher.
        // Speed up the extraction by a large amount by filtering out the files which already exist and dont need updating.
        filteredEntries = new(filteredEntries.Where(x => Globals.Filters.Contains("*.*") || Globals.Filters.Any(o => x.Name.Contains(o))).Where(x =>
        {
            FileInfo f = new(Path.Join(Globals.OutDirectory.FullName, x.Name));
            bool isDecompressable = x.CanDecompress;
            bool isLocked = x.IsCrypted;
            bool fileExists = f.Exists;
            long fileLength = fileExists ? f.Length : 0L;
            if (fileExists && !Globals.ForceOverwrite && !Globals.DeleteOutput)
            {
                additionalFiles = true;
                if (bytesSize - fileLength > 0L) bytesSize -= fileLength;
                else bytesSize = 0L;
            }
            else
            {
                bytesSize += x.Size;
                if (!isDecompressable) isDecompressableCount++;
                if (isLocked) isLockedCount++;
            }
            return isDecompressable && !isLocked && (Globals.ForceOverwrite || Globals.DeleteOutput || !fileExists || fileLength != x.Size);
        }).OrderBy(x => x.Name));

        // Move Game.dcb to the back of the list
        if (Globals.Filters.Contains("*.*") || Globals.Filters.Contains("Game.dcb"))
        {
            ZipEntry e = filteredEntries.First(x => x.Name == "Game.dcb");
            List<ZipEntry> list = filteredEntries.ToList();
            list.Remove(e);
            filteredEntries = new(list);
        }

        // Clear what isnt needed, unp4k/unforge can use large amounts of RAM.
        loadingTrigger = false;
        Logger.NewLine();
    }

    internal static async Task ProvideSummary()
    {
        DriveInfo outputDrive = DriveInfo.GetDrives().First(x => OS.IsWindows ? x.Name == Globals.OutDirectory.FullName[..3] : new DirectoryInfo(x.Name).Exists);
        string summary =
                @"                  \" + '\n' +
                $"                   |                     Output Path | {Globals.OutDirectory.FullName}" + '\n' +
                $"                   |                       Partition | {outputDrive.Name}" + '\n' +
                $"                   |      Partition Total Free Space | {outputDrive.TotalFreeSpace / 1000000000D:0,0.00000000} GB" + '\n' +
                $"                   |  Partition Available Free Space | {outputDrive.AvailableFreeSpace / 1000000000D:0,0.00000000} GB" + '\n' +
                $"                   |        Estimated Required Space | {(!Globals.ForceOverwrite && additionalFiles ? "An Additional " : string.Empty)}" +
                                                                                $"{bytesSize / 1000000000D:0,0.00000000} GB" + '\n' +
                 "                   |                                 | " + '\n' +
                $"                   |                      File Count | {filteredEntries.Count}" +
                                                                                $"{(!Globals.ForceOverwrite && additionalFiles ? " Additional Files" : string.Empty)}" +
                                                                                $"{(Globals.Filters[0] != "*.*" ? $" Filtered From {string.Join(",", Globals.Filters)}" : string.Empty)}" + '\n' +
                $"                   |              Files Incompatible | {isDecompressableCount}" +
                                                                                $"{(!Globals.ForceOverwrite && additionalFiles ? " Additional Files" : string.Empty)}" +
                                                                                $"{(Globals.Filters[0] != "*.*" ? $" Filtered From {string.Join(",", Globals.Filters)}" : string.Empty)}" + '\n' +
                $"                   |                    Files Locked | {isLockedCount}" +
                                                                                $"{(!Globals.ForceOverwrite && additionalFiles ? " Additional Files" : string.Empty)}" +
                                                                                $"{(Globals.Filters[0] != "*.*" ? $" Filtered From {string.Join(",", Globals.Filters)}" : string.Empty)}" + '\n' +
                 "                   |                                 | " + '\n' +
                $"                   |       Combine Extraction Passes | {Globals.CombinePasses}" + '\n' +
                $"                   |   Will Overwrite Existing Files | {Globals.ForceOverwrite}" + '\n' +
                $"                   | Will Perform Special Extraction | {Globals.ShouldSmelt}" + '\n' +
                $"                   |    Will Delete Output Directory | {Globals.DeleteOutput}" + '\n' +
                @"                  /";
        // Never allow the extraction to go through if the target storage drive has too little available space.
        if (outputDrive.AvailableFreeSpace + (Globals.ForceOverwrite || Globals.DeleteOutput ? Globals.OutDirectory.GetFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length) : 0) < bytesSize)
        {
            Logger.LogError("The output path you have chosen is on a partition which does not have enough available free space!" + '\n' + summary);
            Console.ReadKey();
            Globals.ExitTrigger = true;
            return;
        }
        else Logger.NewLine();

        // Give the user a summary of what unp4k/unforge is about to do and some statistics.
        char? goAheadWithExtraction = null;
        while (goAheadWithExtraction is null)
        {
            Logger.LogInfo("Pre-Process Summary" + '\n' + summary);
            Logger.NewLine();
            Console.Write("Should the extraction go ahead? y/n: ");
            goAheadWithExtraction = Console.ReadKey().KeyChar.ToString().ToLower()[0];
            if (goAheadWithExtraction is null || goAheadWithExtraction != 'y' && goAheadWithExtraction != 'n')
            {
                Console.Error.WriteLine("Please input y for yes or n for no! You will be asked again in 3 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(3));
                Logger.ClearBuffer();
                goAheadWithExtraction = null;
            }
            else if (goAheadWithExtraction is 'n')
            {
                Globals.ExitTrigger = true;
                return;
            }
        }
    }

    internal static void DoExtraction()
    {
        Logger.ClearBuffer();

        if (Globals.DeleteOutput)
        {
            Logger.LogInfo($"Deleting {Globals.OutDirectory} - This may take a while...");
            if (Globals.OutDirectory.Exists)
            {
                bool loadingTrigger = true;
                Task.Run(async () =>
                {
                    Console.Write("Processing...");
                    while (loadingTrigger)
                    {
                        Console.Write('.');
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                });
                Globals.OutDirectory.Delete(true);
                loadingTrigger = false;
            }
            Globals.OutDirectory.Create();
            Globals.SmelterOutDirectory.Create();
        }

        // Time the extraction for those who are interested in it.
        Stopwatch overallTime = new();
        overallTime.Start();

        // Do all the extraction things!
        Logger.NewLine(2);
        int tasksCompleted = 0;
        if (!filteredEntries.IsEmpty)
        {
            Parallel.ForEach(filteredEntries, async entry =>
            {
                Logger.LogInfo($"           - Extracting: {entry.Name}");
                FileInfo extractedFile = new(Path.Join(Globals.OutDirectory.FullName, entry.Name));
                string percentage = (tasksCompleted is 0 ? 0D : 100D * tasksCompleted / filteredEntries.Count).ToString("000.00000");
                if (!extractedFile.Directory.Exists) extractedFile.Directory.Create();
                Stopwatch fileTime = new();
                fileTime.Start();

                FileStream fs = extractedFile.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite); // Dont want people accessing incomplete files.
                Stream decompStream = pak.GetInputStream(entry);
                StreamUtils.Copy(decompStream, fs, decomBuffer);
                decompStream.Close();
                fs.Close();
                if (Globals.ShouldSmelt && Globals.CombinePasses) await Smelt(extractedFile, new(Path.Join(Globals.SmelterOutDirectory.FullName, entry.Name)));

                fileTime.Stop();
                if (Globals.DetailedLogs)
                {
                    Logger.LogInfo($"{percentage}% - Extracted:  {entry.Name}" + '\n' +
                        @"                  \" + '\n' +
                        $"                   | Date Last Modified: {entry.DateTime}" + '\n' +
                        $"                   | Compression Method: {entry.CompressionMethod}" + '\n' +
                        $"                   | Compressed Size:    {entry.CompressedSize  / 1000000000D:0,0.000000000000} GB" + '\n' +
                        $"                   | Uncompressed Size:  {entry.Size            / 1000000000D:0,0.000000000000} GB" + '\n' +
                        $"                   | Time Taken:         {fileTime.ElapsedMilliseconds / 1000D:0,0.000} seconds" + '\n' +
                        @"                  /");
                }
                else Logger.LogInfo($"{percentage}% - Extracted:  {entry.Name[(entry.Name.LastIndexOf("/") + 1)..]}");
                Interlocked.Increment(ref tasksCompleted);
            });
            if (Globals.ShouldSmelt && !Globals.CombinePasses)
            {
                Logger.NewLine(2);
                Logger.LogInfo("Beginning Second Extraction Pass...");
                Logger.NewLine(2);
                Parallel.ForEach(filteredEntries, async entry =>
                {
                    Logger.LogInfo($"[{(tasksCompleted is 0 ? 0D : 100D * tasksCompleted / filteredEntries.Count):000.00000}%] - Smelting: {entry.Name}");
                    await Smelt(new(Path.Join(Globals.OutDirectory.FullName, entry.Name)), new(Path.Join(Globals.SmelterOutDirectory.FullName, entry.Name)));
                    Interlocked.Increment(ref tasksCompleted);
                });
            }
        }
        else Logger.LogInfo("No extraction work to be done! Skipping...");

        // This is specifically for smelting smeltable files.
        static async Task Smelt(FileInfo extractedFile, FileInfo smeltedFile)
        {
            if (!smeltedFile.Directory.Exists) smeltedFile.Directory.Create();
            try
            {
                if (extractedFile.Extension is ".dcb") await DataForge.ForgeData(new(extractedFile, smeltedFile));
                else await DataForge.SerialiseData(extractedFile, smeltedFile);
            }
            // TODO: Get rid of as many of these exceptions as possible
            catch (ArgumentException e) { FileExtractionError(extractedFile, e); }
            catch (EndOfStreamException e) { FileExtractionError(extractedFile, e); }
            catch (DirectoryNotFoundException e) { FileExtractionError(extractedFile, e); }
            catch (FileNotFoundException e) { FileExtractionError(extractedFile, e); }
            catch (IOException e) { FileExtractionError(extractedFile, e); }
            catch (AggregateException e) { FileExtractionError(extractedFile, e); }
            catch (TargetInvocationException e) { FileExtractionError(extractedFile, e); }
            catch (KeyNotFoundException e) { FileExtractionError(extractedFile, e); }
            catch (IndexOutOfRangeException e) { FileExtractionError(extractedFile, e); }
        }

        // Print out the post summary.
        overallTime.Stop();
        Logger.NewLine(2);
        Logger.LogInfo(
            "Extraction Complete" + '\n' +
            @"\" + '\n' +
            $" |  File Errors: {fileErrors}" + '\n' +
            $" |  Time Taken: {(float)overallTime.ElapsedMilliseconds / 60000:0,0.000} minutes" + '\n' +
             " |  Due to the nature of SSD's/NVMe's, do not excessively (10 times a day etc) run the extraction on an SSD/NVMe. Doing so may dramatically reduce the lifetime of the SSD/NVMe.");
        Logger.NewLine(2);
        Console.Write("Would you like to open the output directory? (Application will close on input) y/n: ");
        char openOutput = Console.ReadKey().KeyChar;
        if (openOutput is 'y') Process.Start(OS.IsWindows ? "explorer" : "nautilus", Globals.OutDirectory.FullName);
    }

    private static void FileExtractionError<T>(FileInfo file, T e) where T : Exception
    {
        if (Globals.PrintErrors) Logger.LogException(e);
        file.Delete();
        fileErrors++;
    }
}
