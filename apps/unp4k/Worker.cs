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
    private static List<ZipEntry> filteredEntries = new();

    private static ZipFile pak;
    private static readonly byte[] decomBuffer = new byte[4096];
    private static int isDecompressableCount = 0;
    private static int isLockedCount = 0;
    private static long bytesSize = 0L;
    private static bool additionalFiles = false;

    private static int tasksCompleted = 0;

    internal static void ProcessGameData()
    {
        Console.Title = $"unp4k: Working on {Globals.P4kFile.FullName}";

        // Setup the stream from the Data.p4k and contain it as an ICSC ZipFile with the appropriate keys then enqueue all zip entries.
        Utils.RunProgressBarAction("Processing Data.p4k, this may take a moment", () => 
        {
            pak = new(Globals.P4kFile.Open(FileMode.Open, FileAccess.Read, FileShare.None)); // The Data.p4k must be locked while it is being read to avoid corruption.
            pak.UseZip64 = UseZip64.On;
            pak.KeysRequired += (object sender, KeysRequiredEventArgs e) => e.Key = new byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };
            foreach (ZipEntry entry in pak) filteredEntries.Add(entry);

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
        });
    }

    internal static async Task ProvideSummary()
    {
        DriveInfo outputDrive = DriveInfo.GetDrives().First(x => OS.IsWindows ? x.Name == Globals.OutDirectory.FullName[..3] : new DirectoryInfo(x.Name).Exists);
        string summary =
                @"                  \" + '\n' +
                $"                   |                     Output Path | {Globals.OutDirectory.FullName}" + '\n' +
                $"                   |                       Partition | {outputDrive.Name}" + '\n' +
                $"                   |      Partition Total Free Space | {outputDrive.TotalFreeSpace / 1000000000D:0,0.000000000} GB" + '\n' +
                $"                   |  Partition Available Free Space | {outputDrive.AvailableFreeSpace / 1000000000D:0,0.000000000} GB" + '\n' +
                $"                   |        Estimated Required Space | {(!Globals.ForceOverwrite && additionalFiles ? "An Additional " : string.Empty)}" +
                                                                                $"{bytesSize / 1000000000D:0,0.000000000} GB" + '\n' +
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
                $"                   |   Will Overwrite Existing Files | {Globals.ForceOverwrite}" + '\n' +
                $"                   |    Will Delete Output Directory | {Globals.DeleteOutput}" + '\n' +
                $"                   | Will Perform Special Extraction | {Globals.ShouldSmelt}" + '\n' +
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
        Logger.LogInfo("Pre-Process Summary" + '\n' + summary);
        if (!Utils.AskUserInput("Proceed?"))
        {
            Globals.ExitTrigger = true;
            return;
        }
    }

    internal static async Task DoExtraction()
    {
        if (Globals.DeleteOutput && Globals.OutDirectory.Exists)
        {
            Utils.RunProgressBarAction($"Deleting {Globals.OutDirectory} - This may take a while", () => Globals.OutDirectory.Delete(true));
            Globals.OutDirectory.Create();
            Globals.SmelterOutDirectory.Create();
        }
        Logger.ClearBuffer();

        // Time the extraction for those who are interested in it.
        Stopwatch overallTime = new();
        overallTime.Start();

        // Do all the extraction things!
        Logger.NewLine(2);
        if (filteredEntries.Count is not 0) await Task.Run(() => Parallel.ForEach(filteredEntries.AsParallel().AsOrdered(), ProcessEntry));
        else Logger.LogInfo("No extraction work to be done!");

        // This is specifically for smelting smeltable files.
        static async void ProcessEntry(ZipEntry entry, ParallelLoopState state, long id)
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
            if (Globals.ShouldSmelt)
            {
                FileInfo smeltedFile = new(Path.Join(Globals.SmelterOutDirectory.FullName, entry.Name));
                try
                {
                    if (extractedFile.Extension is ".dcb") await DataForge.ForgeData(new(extractedFile, smeltedFile), Globals.DetailedLogs);
                    else await DataForge.SerialiseData(extractedFile, smeltedFile);
                }
                catch (Exception e)
                {
                    if (Globals.PrintErrors) Logger.LogException(e);
                    if (smeltedFile.Exists) smeltedFile.Delete();
                    Globals.FileErrors++;
                }
            }

            fileTime.Stop();
            Interlocked.Increment(ref tasksCompleted);
            if (Globals.DetailedLogs)
            {
                Logger.LogInfo($"{percentage}% - Extracted:  {entry.Name}" + '\n' +
                    @"                              \" + '\n' +
                    $"                               | Date Last Modified: {entry.DateTime}" + '\n' +
                    $"                               | Compression Method: {entry.CompressionMethod}" + '\n' +
                    $"                               | Compressed Size:    {entry.CompressedSize  / 1000000000D:0,0.000000000} GB" + '\n' +
                    $"                               | Uncompressed Size:  {entry.Size            / 1000000000D:0,0.000000000} GB" + '\n' +
                    $"                               | Time Taken:         {fileTime.ElapsedMilliseconds / 1000D:0,0.000} seconds" + '\n' +
                    @"                              /");
            }
            else Logger.LogInfo($"{percentage}% - Extracted:  {entry.Name[(entry.Name.LastIndexOf("/") + 1)..]}");
        }

        // Print out the post summary.
        overallTime.Stop();
        Logger.NewLine();
        Logger.LogInfo(
            "Extraction Complete" + '\n' +
            @"\" + '\n' +
            $" |  File Errors: {Globals.FileErrors}" + '\n' +
            $" |  Time Taken: {(float)overallTime.ElapsedMilliseconds / 60000:0,0.000} minutes" + '\n' +
             " |  Due to the nature of SSD's/NVMe's, do not excessively (10 times a day etc) run the extraction on an SSD/NVMe. Doing so may dramatically reduce the lifetime of the SSD/NVMe.");
        if (Utils.AskUserInput("Would you like to open the output directory? (Application will close on input)")) Process.Start(OS.IsWindows ? "explorer" : "nautilus", Globals.OutDirectory.FullName);
    }
}
