using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Concurrent;

/*
 * TODO: While Linux is supported, we need to add in everything when Star Citizen becomes available on Linux
 */

#region Initialisation

string? appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
string? defaultp4kPath = @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k";
string? defaultExtractionPath = Path.Join(appPath, "star_citizen_extraction");

string? p4kPath = null;
string? outDirectoryPath = null;
List<string> filters = new();

bool detailedLogs = false;

Logger.ClearBuffer();
Logger.LogInfo("Initialising...");

if (appPath is null)
{
    Logger.LogError("Could not discern application path! Cannot continue!");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

if (args.Length == 0) 
{
    p4kPath = "Data.p4k";
    outDirectoryPath = defaultExtractionPath;
    filters.Add("*.*");
    Logger.Log("################################################################################\n");
    Logger.Log("                             unp4ck <> Star Citizen                             ");
    Logger.Log(
        "\nExtracts Star Citizen's Data.p4k into a directory of choice\n"
        );
    Logger.NewLine();
    Logger.Log(@"Windows PowerShell: .\unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' + 
        " -f " + '"' + "[filter(Example: *.* for all files, this is the default)]" + '"');
    Logger.Log(@"Windows Command Prompt: unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' +
        " -f " + '"' + "[filter(Example: *.* for all files, this is the default)]" + '"');
    Logger.Log(@"Linux Terminal: ./unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' +
        " -f " + '"' + "[filter(Example: *.* for all files, this is the default)]" + '"');
    Logger.NewLine();
    Logger.Log(@"A Windows Example: unp4ck -i " + '"' + @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k" + '"' + 
        " -o " + '"' + @"C:\Windows\SC" + '"' + 
        " -f " + '"' + "*.*" + '"' + " -d");
    Logger.Log("-d: Enables the detailed logging mode.");
    Logger.Log("-i: Delcares the input file path.");
    Logger.Log("-o: Declared the output directory path.");
    Logger.Log("-f: Allows you to filter in the files you want.");
    Logger.NewLine();
    Logger.Log("File Type Selection: .dcb");
    Logger.Log("Multi-File Type Selection: .dcb,.png,.gif");
    Logger.Log("Specific File Selection: Game.dcb");
    Logger.Log("Multi-Specific File Selection: Game.dcb,smiley_face.png,its_working.gif");
    Logger.Log("\n################################################################################\n");
    Logger.LogWarn($"\nNO INPUT Data.p4k PATH HAS BEEN DECLARED. USING DEFAULT PATH " + '"' + $"{defaultp4kPath}" + '"');
    Logger.LogWarn("\nNO OUTPUT DIRECTORY PATH HAS BEEN DECLARED. ALL EXTRACTS WILL GO INTO " + '"' + $"{defaultExtractionPath}" + '"');
    Logger.Log("\nPress any key to continue!");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

try
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i].ToLowerInvariant() == "-i") p4kPath = args[i + 1];
        else if (args[i].ToLowerInvariant() == "-o") outDirectoryPath = args[i + 1];
        else if (args[i].ToLowerInvariant() == "-f") filters = args[i + 1].Split(',').ToList();
        else if (args[i].ToLowerInvariant() == "-d") detailedLogs = true;
    }
}
catch (IndexOutOfRangeException e)
{
    Logger.LogException(e);
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

if (p4kPath is null) p4kPath = defaultp4kPath;
if (outDirectoryPath is null) outDirectoryPath = defaultExtractionPath;
if (filters.Count == 0) filters.Add("*.*");

if (!File.Exists(p4kPath))
{
    Logger.LogError($"Input path '{p4kPath}' does not exist!");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}
if (!Directory.Exists(outDirectoryPath))
{
    Logger.LogError($"Output path '{outDirectoryPath}' does not exist!");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

#endregion

#region Program

Logger.ClearBuffer();
Logger.LogInfo("Processing Data.p4k before extraction...");

FileInfo p4k = new(p4kPath);
byte[] decomBuffer = new byte[4096];

ConcurrentQueue<ZipEntry> filteredEntries = new();
using FileStream p4kStream = p4k.Open(FileMode.Open, FileAccess.Read, FileShare.None); // The Data.p4k must be locked while it is being read to avoid corruption.
ZipFile pak = new(p4kStream);
pak.KeysRequired += (object sender, KeysRequiredEventArgs e) => e.Key = new byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };

foreach (ZipEntry entry in pak) filteredEntries.Enqueue(entry);
if (filters[0] == "*.*") filteredEntries = new(filteredEntries.OrderBy(x => x.Name));
else
{
    ConcurrentQueue<ZipEntry> filtered = new();
    foreach (string filt in filters) filtered = new(filtered.Concat(filteredEntries.Where(x => x.Name.ToLowerInvariant().Contains(filt.ToLowerInvariant()))));
    filteredEntries = filtered;
}
filteredEntries = new(filteredEntries.OrderBy(x => x.Name));

int cannotDecompress = 0;
int lockedCount = 0;
int fileCount = 0;
long bytesSize = 0L;
foreach (ZipEntry entry in filteredEntries)
{
    if (entry.CanDecompress && !entry.IsCrypted && !entry.IsAesCrypted)
    {
        if (entry.Size != -1) bytesSize += entry.Size;
        fileCount++;
    }
    else if (!entry.CanDecompress) cannotDecompress++;
    else if (entry.IsCrypted || entry.IsAesCrypted) lockedCount++;
}

Logger.ClearBuffer();

DriveInfo outputDrive = DriveInfo.GetDrives().First(x => x.Name == outDirectoryPath.Substring(0, 3));
if (outputDrive.AvailableFreeSpace < bytesSize)
{
    Logger.LogError("- The output path you have chosen is on a storage drive which does not have enough available free space!");
    Logger.LogInfo(@"| \");
    Logger.LogError($"|  | Output Path: {outDirectoryPath}");
    Logger.LogError($"|  | Selected Drive Partition: {outputDrive.Name}");
    Logger.LogError($"|  | Selected Drive Partition Total Free Space:     {outputDrive.TotalFreeSpace:#,#.###} Bytes  :  {(float)outputDrive.TotalFreeSpace / 1000000:#,#.###} MB  :  {(float)outputDrive.TotalFreeSpace / 1000000000:#,#.###} GB");
    Logger.LogError($"|  | Selected Drive Partition Available Free Space: {outputDrive.AvailableFreeSpace:#,#.###} Bytes  :  {(float)outputDrive.AvailableFreeSpace / 1000000:#,#.###} MB  :  {(float)outputDrive.AvailableFreeSpace / 1000000000:#,#.###} GB");
    Logger.LogError($"|  | Extraction Required Space:           {bytesSize:#,#.###} Bytes  :  {(float)bytesSize / 1000000:#,#.###} MB  :  {(float)bytesSize / 1000000000:#,#.###} GB");
    Logger.LogError($"|  | File Count: {fileCount}{(filters[0] != "*.*" ? $" filtered from {string.Join(",", filters)}" : string.Empty)}");
    Logger.LogError($"|  | Files Cannot Be Decompressed: {cannotDecompress}");
    Logger.LogError($"|  | Files Locked: {lockedCount}");
    Logger.LogInfo(@"| /");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

char? confirm = null;
while (confirm is null)
{
    Logger.LogInfo("- Extraction Details");
    Logger.LogInfo(@"| \");
    Logger.LogInfo($"|  | Output Path: {outDirectoryPath}");
    Logger.LogInfo($"|  | Selected Drive Partition: {outputDrive.Name}");
    Logger.LogInfo($"|  | Selected Drive Partition Total Free Space:     {(float)outputDrive.TotalFreeSpace / 1000:#,#.###} KB  :  {(float)outputDrive.TotalFreeSpace / 1000000:#,#.###} MB  :  {(float)outputDrive.TotalFreeSpace / 1000000000:#,#.###} GB");
    Logger.LogInfo($"|  | Selected Drive Partition Available Free Space: {(float)outputDrive.AvailableFreeSpace / 1000:#,#.###} KB  :  {(float)outputDrive.AvailableFreeSpace / 1000000:#,#.###} MB  :  {(float)outputDrive.AvailableFreeSpace / 1000000000:#,#.###} GB");
    Logger.LogInfo($"|  | Extraction Required Space:           {(float)bytesSize / 1000:#,#.###} KB  :  {(float)bytesSize / 1000000:#,#.###} MB  :  {(float)bytesSize / 1000000000:#,#.###} GB");
    Logger.LogInfo($"|  | File Count: {fileCount}{(filters[0] != "*.*" ? $" filtered from {string.Join(",", filters)}": string.Empty)}");
    Logger.LogInfo($"|  | Files Cannot Be Decompressed: {cannotDecompress}");
    Logger.LogInfo($"|  | Files Locked: {lockedCount}");
    Logger.LogInfo(@"| /");
    Logger.NewLine();
    Logger.LogInfo("Should the extraction go ahead? y/n: ");
    confirm = Console.ReadKey().KeyChar;
    if (confirm is null || confirm != 'y' && confirm != 'n')
    {
        Logger.LogError("Please input y for yes or n for no!");
        Thread.Sleep(TimeSpan.FromSeconds(3));
        Logger.ClearBuffer();
        confirm = null;
    }
    else if (confirm == 'n')
    {
        Logger.ClearBuffer();
        Environment.Exit(0);
    }
}

Logger.ClearBuffer();

string? currentDir = null;
int taskCount = 16;
List<Task> tasks = new();
Stopwatch watch = new();
watch.Start();
Parallel.ForEach(filteredEntries, (entry) => 
{
    if (entry.CanDecompress)
    {
        FileInfo inFile = new(Path.Join(outDirectoryPath, entry.Name));
        DirectoryInfo dir = inFile.Directory;
        if (!dir.Exists)
        {
            Logger.LogInfo($"- Creating Directory: {entry.Name.Substring(0, entry.Name.LastIndexOf("/") + 1)}");
            dir.Create();
        }
        else if (currentDir is not null && currentDir != dir.FullName) Logger.LogInfo($"- Using Directory: {currentDir = dir.FullName}");
        Logger.LogInfo($"| - {(entry.IsCrypted || entry.IsAesCrypted || inFile.Exists ? "Skipping" : "Extracting")} File: {entry.Name[(entry.Name.LastIndexOf("/") + 1)..]}");
        if (detailedLogs)
        {
            Logger.LogInfo(@"|   \");
            Logger.LogInfo($"|    | Date Last Modified: {entry.DateTime}");
            Logger.LogInfo($"|    | Is Locked: {entry.IsCrypted || entry.IsAesCrypted}");
            Logger.LogInfo($"|    | Compression Method: {entry.CompressionMethod}");
            Logger.LogInfo($"|    | Compressed Size:   {(float)entry.CompressedSize / 1000:#,#.###} KB : {(float)entry.CompressedSize / 1000000:#,#.######} MB  :  {(float)entry.CompressedSize / 1000000000:#,#.#########} GB");
            Logger.LogInfo($"|    | Uncompressed Size: {(float)entry.Size / 1000:#,#.###} KB : {(float)entry.Size / 1000000:#,#.######} MB  :  {(float)entry.Size / 1000000000:#,#.#########} GB");
            Logger.LogInfo(@"|   /");
        }
        if (!entry.IsCrypted && !entry.IsAesCrypted && (!inFile.Exists || inFile.Length != entry.Size))
        {
            try
            {
                FileInfo outFile = new(Path.Join(outDirectoryPath, entry.Name));
                using FileStream fs = outFile.Open(FileMode.Create, FileAccess.Write, FileShare.None); // Dont want people accessing incomplete files.
                using Stream s = pak.GetInputStream(entry);
                StreamUtils.Copy(s, fs, decomBuffer);
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.LogException(e);
            }
            catch (FileNotFoundException e)
            {
                Logger.LogException(e);
            }
            catch (IOException e)
            {
                Logger.LogException(e);
            }
        }
    }
});
watch.Stop();

Logger.NewLine(2);
Logger.LogInfo("- Extraction Completed!");
Logger.LogInfo(@" \");
Logger.LogInfo($"  |  Time Taken: {(float)watch.ElapsedMilliseconds / 60000:#,#.###} minutes");
Logger.LogWarn("  |  Due to the nature of SSD's/NVMe's, do not excessively run the extraction on an SSD/NVMe. Doing so may reduce the lifetime of the SSD/NVMe.");
Logger.NewLine(2);
Logger.Log("Would you like to open the output directory? (Application will close on input) y/n: ");
char openOutput = Console.ReadKey().KeyChar;
if (openOutput == 'y') Process.Start("explorer.exe", outDirectoryPath);

#endregion Program