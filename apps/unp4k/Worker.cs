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
    private static ConcurrentQueue<ZipEntry> existenceFilteredExtractionEntries = new();
    private static ConcurrentQueue<ZipEntry> existenceFilteredSmeltingEntries = new();

    private static ZipFile pak;
    private static readonly byte[] decomBuffer = new byte[4096];
    private static int isDecompressableCount = 0;
    private static int isLockedCount = 0;
    private static long bytesSize = 0L;

    internal static async Task ProcessGameData()
    {
        Console.Title = $"unp4k: Working on {Globals.P4kFile.FullName}";

        // Setup the stream from the Data.p4k and contain it as an ICSC ZipFile with the appropriate keys then enqueue all zip entries.
        Logger.LogInfo($"[0%] Processing Data.p4k before extraction{(Globals.ShouldSmelt ? " and smelting" : string.Empty)}, this may take a while...");
        pak = new(Globals.P4kFile.Open(FileMode.Open, FileAccess.Read, FileShare.None));// The Data.p4k must be locked while it is being read to avoid corruption.
        pak.KeysRequired += (object sender, KeysRequiredEventArgs e) => e.Key = new byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };
        foreach (ZipEntry entry in pak) filteredEntries.Enqueue(entry);

        // Filter out zip entries which cannot be decompressed and/or are locked behind a cypher.
        Logger.LogInfo($"[33%] Testing Data.p4k Entry Integrity...");
        filteredEntries = new(filteredEntries.Where(x => Globals.Filters.Contains("*.*") || Globals.Filters.Any(o => x.Name.Contains(o))).Where(x =>
        {
            bool isDecompressable = x.CanDecompress;
            bool isLocked = x.IsCrypted || x.IsAesCrypted;
            if (isDecompressable) isDecompressableCount++;
            if (isLocked) isLockedCount++;
            return isDecompressable && !isLocked;
        }).OrderBy(x => x.Name));

        // Speed up the extraction by a large amount by filtering out the files which already exist and dont need updating.
        Logger.LogInfo($"[66%] Optimising Extractable File List...");
        existenceFilteredExtractionEntries = new(filteredEntries.Where(x =>
        {
            FileInfo f = new(Path.Join(Globals.OutDirectory.FullName, x.Name));
            if (f.Exists) bytesSize -= f.Length;
            else bytesSize += x.Size;
            return Globals.ForceOverwrite || !f.Exists || f.Length != x.Size;
        }));
        existenceFilteredSmeltingEntries = existenceFilteredExtractionEntries;

        // Clear what isnt needed, unp4k/unforge can use large amounts of RAM.
        Logger.LogInfo($"[100%] Flushing Waste Data From RAM...");
        filteredEntries.Clear();
    }

    internal static async Task ProvideSummary()
    {
        bool additionalFiles = false;
        DriveInfo outputDrive = DriveInfo.GetDrives().First(x => OS.IsWindows ? x.Name == Globals.OutDirectory.FullName[..3] : new DirectoryInfo(x.Name).Exists);
        string summary =
                @"                     \" + '\n' +
                $"                      |                    Output Path | {Globals.OutDirectory.FullName}" + '\n' +
                $"                      |                      Partition | {outputDrive.Name}" + '\n' +
                $"                      |     Partition Total Free Space | {outputDrive.TotalFreeSpace / 1000000000D:0,0.00000} GB" + '\n' +
                $"                      | Partition Available Free Space | {outputDrive.AvailableFreeSpace / 1000000000D:0,0.00000} GB" + '\n' +
                $"                      |       Estimated Required Space | {(additionalFiles ? "An Additional " : string.Empty)}" +
                                                                                $"{bytesSize / 1000000000D:0,0.00000} GB" +
                                                                                $"{(Globals.ShouldSmelt ? " Excluding Smeltable Files" : string.Empty)}" + '\n' +
                 "                      |                                | " + '\n' +
                $"                      |                     File Count | {existenceFilteredExtractionEntries.Count}" +
                                                                                $"{(additionalFiles ? " Additional Files" : string.Empty)}" +
                                                                                $"{(Globals.Filters[0] != "*.*" ? $" Filtered From {string.Join(",", Globals.Filters)}" : string.Empty)}" +
                                                                                $"{(Globals.ShouldSmelt ? " Excluding Smeltable Files" : string.Empty)}" + '\n' +
                $"                      |             Files Incompatible | {isDecompressableCount}" +
                                                                                $"{(additionalFiles ? " Additional Files" : string.Empty)}" +
                                                                                $"{(Globals.Filters[0] != "*.*" ? $" Filtered From {string.Join(",", Globals.Filters)}" : string.Empty)}" +
                                                                                $"{(Globals.ShouldSmelt ? " Excluding Smeltable Files" : string.Empty)}" + '\n' +
                $"                      |                   Files Locked | {isLockedCount}" +
                                                                                $"{(additionalFiles ? " Additional Files" : string.Empty)}" +
                                                                                $"{(Globals.Filters[0] != "*.*" ? $" Filtered From {string.Join(",", Globals.Filters)}" : string.Empty)}" +
                                                                                $"{(Globals.ShouldSmelt ? " Excluding Smeltable Files" : string.Empty)}" + '\n' +
                 "                      |                                | " + '\n' +
                $"                      | Combine Extract & Smelt Passes | {Globals.CombinePasses}" + '\n' +
                $"                      |     Will Smelt Extracted Files | {Globals.ShouldSmelt}" + '\n' +
                @"                     /";
        // Never allow the extraction to go through if the target storage drive has too little available space.
        if (outputDrive.AvailableFreeSpace < bytesSize)
        {
            Logger.LogError("| - The output path you have chosen is on a partition which does not have enough available free space!" + '\n' + summary);
            Console.ReadKey();
            Logger.ClearBuffer();
            Environment.Exit(0);
        }
        else Logger.NewLine();

        // Give the user a summary of what unp4k/unforge is about to do and some statistics.
        char? goAheadWithExtraction = null;
        while (goAheadWithExtraction is null)
        {
            Logger.LogInfo("| - Pre-Process Summary" + '\n' + summary);
            Logger.NewLine();
            Logger.LogInfo("Should the extraction go ahead? y/n: ");
            goAheadWithExtraction = Console.ReadKey().KeyChar;
            if (goAheadWithExtraction is null || goAheadWithExtraction != 'y' && goAheadWithExtraction != 'n')
            {
                Logger.LogError("Please input y for yes or n for no! You will be asked again in 3 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(3));
                Logger.ClearBuffer();
                goAheadWithExtraction = null;
            }
            else if (goAheadWithExtraction is 'n')
            {
                Logger.ClearBuffer();
                Environment.Exit(0);
            }
        }
    }

    internal static async Task DoExtraction()
    {
        Logger.ClearBuffer();

        // Time the extraction for those who are interested in it.
        Stopwatch overallTime = new();
        overallTime.Start();

        // Do all the extraction things!
        Logger.NewLine(2);
        if (Globals.ShouldSmelt && !Globals.CombinePasses) Logger.LogInfo("Beginning Extraction Pass...");
        Logger.LogInfo($"Beginning Extraction{(Globals.ShouldSmelt && Globals.CombinePasses ? " & Smelting" : string.Empty)} Pass...");
        Logger.NewLine(2);
        if (!existenceFilteredExtractionEntries.IsEmpty)
        {
            int tasksCompleted = 0;
            Parallel.ForEach(existenceFilteredExtractionEntries, entry =>
            {
                FileInfo extractedFile = new(Path.Join(Globals.OutDirectory.FullName, entry.Name));
                string percentage = (tasksCompleted is 0 ? 0D : 100D * tasksCompleted / existenceFilteredExtractionEntries.Count).ToString("000.00000");
                if (!extractedFile.Directory.Exists) extractedFile.Directory.Create();
                Stopwatch fileTime = new();
                fileTime.Start();
                try
                {
                    tasksCompleted++;
                    FileStream fs = extractedFile.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite); // Dont want people accessing incomplete files.
                    Stream decompStream = pak.GetInputStream(entry);
                    StreamUtils.Copy(decompStream, fs, decomBuffer);
                    decompStream.Close();
                    fs.Close();
                    if (Globals.ShouldSmelt && Globals.CombinePasses) Smelt(extractedFile, new(Path.Join(Globals.SmelterOutDirectory.FullName, entry.Name)));
                }
                // TODO: Get rid of as many of these exceptions as possible
                catch (DirectoryNotFoundException e)
                {
                    if (Globals.PrintErrors) Logger.LogException(e);
                }
                catch (FileNotFoundException e)
                {
                    if (Globals.PrintErrors) Logger.LogException(e);
                }
                catch (IOException e)
                {
                    if (Globals.PrintErrors) Logger.LogException(e);
                }
                catch (AggregateException e)
                {
                    if (Globals.PrintErrors) Logger.LogException(e);
                }
                finally
                {
                    fileTime.Stop();
                    if (Globals.DetailedLogs)
                    {
                        Logger.LogInfo($"| [{percentage}%] - Extracted{(Globals.CombinePasses ? " & Smelted" : string.Empty)}: {entry.Name}" + '\n' +
                            @"                    \" + '\n' +
                            $"                     | Date Last Modified: {entry.DateTime}" + '\n' +
                            $"                     | Compression Method: {entry.CompressionMethod}" + '\n' +
                            $"                     | Compressed Size:    {entry.CompressedSize  / 1000000000D:0,0.000000000000} GB" + '\n' +
                            $"                     | Uncompressed Size:  {entry.Size            / 1000000000D:0,0.000000000000} GB" + '\n' +
                            $"                     | Time Taken:         {(float)fileTime.ElapsedMilliseconds / 1000:#,#.####} seconds" + '\n' +
                            @"                    /");
                    }
                    else Logger.LogInfo($"| [{percentage}%] - Extracted{(Globals.CombinePasses ? " & Smelted" : string.Empty)}: {entry.Name[(entry.Name.LastIndexOf("/") + 1)..]}");
                }
            });
        }
        else Logger.LogInfo("No extraction work to be done! Skipping...");
        if (!existenceFilteredSmeltingEntries.IsEmpty)
        {
            if (Globals.ShouldSmelt && !Globals.CombinePasses)
            {
                Logger.NewLine(2);
                Logger.LogInfo("Beginning Smelting Pass...");
                Logger.NewLine(2);
                int tasksCompleted = 0;
                Parallel.ForEach(existenceFilteredSmeltingEntries, entry =>
                {
                    tasksCompleted++;
                    Logger.LogInfo($"| [{(tasksCompleted is 0 ? 0D : 100D * tasksCompleted / existenceFilteredExtractionEntries.Count):000.00000}%] - Smelting: {entry.Name}");
                    Smelt(new(Path.Join(Globals.OutDirectory.FullName, entry.Name)), new(Path.Join(Globals.SmelterOutDirectory.FullName, entry.Name)));
                });
            }
        }
        else Logger.LogInfo("No smelting work to be done! Skipping...");

        // This is specifically for smelting smeltable files.
        static void Smelt(FileInfo extractedFile, FileInfo smeltedFile)
        {
            if (!smeltedFile.Directory.Exists) smeltedFile.Directory.Create();
            try
            {
                if (extractedFile.Extension is ".dcb") DataForge.Forge(new(extractedFile, smeltedFile)).GetAwaiter().GetResult();
                else new CryXmlSerializer(extractedFile).Save(smeltedFile);
            }
            // TODO: Get rid of as many of these exceptions as possible
            catch (ArgumentException e)
            {
                if (Globals.PrintErrors) Logger.LogException(e);
            }
            catch (EndOfStreamException e)
            {
                if (Globals.PrintErrors) Logger.LogException(e);
            }
            catch (DirectoryNotFoundException e)
            {
                if (Globals.PrintErrors) Logger.LogException(e);
            }
            catch (FileNotFoundException e)
            {
                if (Globals.PrintErrors) Logger.LogException(e);
            }
            catch (IOException e)
            {
                if (Globals.PrintErrors) Logger.LogException(e);
            }
            catch (AggregateException e)
            {
                if (Globals.PrintErrors) Logger.LogException(e);
            }
            catch (TargetInvocationException e)
            {
                if (Globals.PrintErrors) Logger.LogException(e);
            }
            catch (KeyNotFoundException e)
            {
                if (Globals.PrintErrors) Logger.LogException(e);
            }
        }

        // Print out the post summary.
        overallTime.Stop();
        Logger.NewLine(2);
        Logger.LogInfo("- Extraction Completed!");
        Logger.LogInfo(@" \");
        Logger.LogInfo($"  |  Time Taken: {(float)overallTime.ElapsedMilliseconds / 60000:#,#.###} minutes");
        Logger.LogWarn("  |  Due to the nature of SSD's/NVMe's, do not excessively (10 times a day etc) run the extraction on an SSD/NVMe. Doing so may reduce the lifetime of the SSD/NVMe.");
        Logger.NewLine(2);
        Logger.LogInfo("Would you like to open the output directory? (Application will close on input) y/n: ");
        char openOutput = Console.ReadKey().KeyChar;
        if (openOutput is 'y') Process.Start(OS.IsWindows ? "explorer" : "nautilus", Globals.OutDirectory.FullName);
    }
}
