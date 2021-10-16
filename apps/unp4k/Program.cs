﻿using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Concurrent;

using unforge;

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

/*
 * TODO: While Linux is supported, we need to add in everything when Star Citizen becomes available on Linux
 */

#region Initialisation

DirectoryInfo? appPath = new(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
FileInfo? defaultp4kFile = new(@"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k");
DirectoryInfo? defaultExtractionDirectory = new(Path.Join(appPath.FullName, "star_citizen_extraction"));

FileInfo? p4kFile = null;
DirectoryInfo? outDirectory = null;
DirectoryInfo? smelterOutDirectory = null;
List<string> filters = new();

bool detailedLogs = false;
bool shouldSmelt = false;

Logger.ClearBuffer();
Logger.LogInfo("Initialising...");

if (appPath is null)
{
    Logger.LogError("Could not discern application path! Cannot continue!");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

if (args.Length is 0) 
{
    p4kFile = new("Data.p4k");
    outDirectory = defaultExtractionDirectory;
    filters.Add("*.*");
    Logger.LogInfo("################################################################################\n");
    Logger.LogInfo("                             unp4ck <> Star Citizen                             ");
    Logger.LogInfo(
        "\nExtracts Star Citizen's Data.p4k into a directory of choice and even convert them into xml files!\n"
        );
    Logger.NewLine();
    Logger.LogInfo(@"Windows PowerShell: .\unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' + 
        " -f " + '"' + "[filter(Example: *.* for all files, this is the default)]" + '"');
    Logger.LogInfo(@"Windows Command Prompt: unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' +
        " -f " + '"' + "[filter(Example: *.* for all files, this is the default)]" + '"');
    Logger.LogInfo(@"Linux Terminal: ./unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' +
        " -f " + '"' + "[filter(Example: *.* for all files, this is the default)]" + '"');
    Logger.NewLine();
    Logger.LogInfo(@"A Windows Example: unp4ck -i " + '"' + @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k" + '"' + 
        " -o " + '"' + @"C:\Windows\SC" + '"' + 
        " -f " + '"' + "*.*" + '"' + " -d");
    Logger.LogInfo("-d: Enables the detailed logging mode.");
    Logger.LogInfo("-i: Delcares the input file path.");
    Logger.LogInfo("-o: Declared the output directory path.");
    Logger.LogInfo("-f: Allows you to filter in the files you want.");
    Logger.NewLine();
    Logger.LogInfo("File Type Selection: .dcb");
    Logger.LogInfo("Multi-File Type Selection: .dcb,.png,.gif");
    Logger.LogInfo("Specific File Selection: Game.dcb");
    Logger.LogInfo("Multi-Specific File Selection: Game.dcb,smiley_face.png,its_working.gif");
    Logger.LogInfo("\n################################################################################\n");
    Logger.LogWarn($"\nNO INPUT Data.p4k PATH HAS BEEN DECLARED. USING DEFAULT PATH " + '"' + $"{defaultp4kFile.FullName}" + '"');
    Logger.LogWarn("\nNO OUTPUT DIRECTORY PATH HAS BEEN DECLARED. ALL EXTRACTS WILL GO INTO " + '"' + $"{defaultExtractionDirectory.FullName}" + '"');
    Logger.LogInfo("\nPress any key to continue!");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

try
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i].ToLowerInvariant() is "-i") p4kFile = new(args[i + 1]);
        else if (args[i].ToLowerInvariant() is "-o") outDirectory = new(args[i + 1]);
        else if (args[i].ToLowerInvariant() is "-f") filters = args[i + 1].Split(',').ToList();
        else if (args[i].ToLowerInvariant() is "-d") detailedLogs = true;

        else if (args[i].ToLowerInvariant() is "-forge") shouldSmelt = true;
    }
}
catch (IndexOutOfRangeException e)
{
    Logger.LogException(e);
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

if (p4kFile is null) p4kFile = defaultp4kFile;
if (outDirectory is null) outDirectory = defaultExtractionDirectory;
if (filters.Count is 0) filters.Add("*.*");

if (!p4kFile.Exists)
{
    Logger.LogError($"Input path '{p4kFile.FullName}' does not exist!");
    Logger.LogError($"Make sure you have the path pointing to a Star Citizen Data.p4k file!");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}
if (!outDirectory.Exists)
{
    Logger.LogError($"Output path '{outDirectory.FullName}' does not exist!");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

smelterOutDirectory = new(Path.Join(outDirectory.FullName, "Smelted"));
if (!smelterOutDirectory.Exists) smelterOutDirectory.Create();

#endregion

#region Program

Console.Title = $"unp4k: Processing {p4kFile.FullName}";
Logger.ClearBuffer();
Logger.LogInfo($"Processing Data.p4k before extraction{(shouldSmelt ? " and smelting" : string.Empty)}, this may take a while...");

byte[] decomBuffer = new byte[4096];

ConcurrentQueue<ZipEntry> filteredEntries = new();
ConcurrentQueue<ZipEntry> existenceFilteredExtractionEntries = new();
ConcurrentQueue<ZipEntry> existenceFilteredSmeltingEntries = new();
using FileStream p4kStream = p4kFile.Open(FileMode.Open, FileAccess.Read, FileShare.None); // The Data.p4k must be locked while it is being read to avoid corruption.
ZipFile pak = new(p4kStream);
pak.KeysRequired += (object sender, KeysRequiredEventArgs e) => e.Key = new byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };

bool additionalFiles = false;
int isDecompressableCount = 0;
int isLockedCount = 0;
long bytesSize = 0L;
foreach (ZipEntry entry in pak) filteredEntries.Enqueue(entry);
filteredEntries = new(filteredEntries.Where(x => filters.Contains("*.*") ? true : filters.Any(o => x.Name.Contains(o))).Where(x =>
{
    bool isDecompressable = x.CanDecompress;
    bool isLocked = x.IsCrypted || x.IsAesCrypted;
    if (isDecompressable) isDecompressableCount++;
    if (isLocked) isLockedCount++;
    return isDecompressable && !isLocked;
}).OrderBy(x => x.Name));
existenceFilteredExtractionEntries = new(filteredEntries.Where(x => !new FileInfo(Path.Join(outDirectory.FullName, x.Name)).Exists));
existenceFilteredSmeltingEntries   = new(filteredEntries.Where(x => !new FileInfo(Path.ChangeExtension(Path.Join(smelterOutDirectory.FullName, x.Name), "xml")).Exists));

Logger.ClearBuffer();
DriveInfo outputDrive = DriveInfo.GetDrives().First(x => x.Name == outDirectory.FullName[..3]);
if (outputDrive.AvailableFreeSpace < bytesSize)
{
    Logger.LogError(
         "| - The output path you have chosen is on a storage drive which does not have enough available free space!" + '\n' +
        @"                              |  \" + '\n' +
        $"                              |   | Output Path: {outDirectory.FullName}" + '\n' +
        $"                              |   | Selected Drive Partition: {outputDrive.Name}" + '\n' +
        $"                              |   | Selected Drive Partition Total Free Space:     {(float)outputDrive.TotalFreeSpace / 1000000:#,#.###} MB  :  {(float)outputDrive.TotalFreeSpace / 1000000000:#,#.###} GB" + '\n' +
        $"                              |   | Selected Drive Partition Available Free Space: {(float)outputDrive.AvailableFreeSpace / 1000000:#,#.###} MB  :  {(float)outputDrive.AvailableFreeSpace / 1000000000:#,#.###} GB" + '\n' +
        $"                              |   | Estimated Required Space:       {(additionalFiles ? "An Additional " : "              ")}{(float)bytesSize / 1000000:#,#.###} MB  :  {(float)bytesSize / 1000000000:#,#.###} GB" +
                                                $"{(shouldSmelt ? " Excluding Smeltable Files" : string.Empty)}" + '\n' +
        $"                              |   | File Count: {existenceFilteredExtractionEntries.Count}{(additionalFiles ? " Additional Files" : string.Empty)}{(filters[0] != "*.*" ? $" Filtered From {string.Join(",", filters)}" : string.Empty)}" +
                                                $"{(shouldSmelt ? " Excluding Smeltable Files" : string.Empty)}" + '\n' +
        $"                              |   | Files Cannot Be Decompressed: {isDecompressableCount}" + '\n' +
        $"                              |   | Files Locked: {isLockedCount}" + '\n' +
        @"                              |  /");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

char? confirm = null;
while (confirm is null)
{
    Logger.LogInfo(
         "| - The output path you have chosen is on a storage drive which does not have enough available free space!" + '\n' +
        @"                              |  \" + '\n' +
        $"                              |   | Output Path: {outDirectory.FullName}" + '\n' +
        $"                              |   | Selected Drive Partition: {outputDrive.Name}" + '\n' +
        $"                              |   | Selected Drive Partition Total Free Space:     {(float)outputDrive.TotalFreeSpace / 1000000:#,#.###} MB  :  {(float)outputDrive.TotalFreeSpace / 1000000000:#,#.###} GB" + '\n' +
        $"                              |   | Selected Drive Partition Available Free Space: {(float)outputDrive.AvailableFreeSpace / 1000000:#,#.###} MB  :  {(float)outputDrive.AvailableFreeSpace / 1000000000:#,#.###} GB" + '\n' +
        $"                              |   | Estimated Required Space:       {(additionalFiles ? "An Additional " : "              ")}{(float)bytesSize / 1000000:#,#.###} MB  :  {(float)bytesSize / 1000000000:#,#.###} GB" +
                                                $"{(shouldSmelt ? " Excluding Smeltable Files" : string.Empty)}" + '\n' +
        $"                              |   | File Count: {existenceFilteredExtractionEntries.Count}{(additionalFiles ? " Additional Files" : string.Empty)}{(filters[0] != "*.*" ? $" Filtered From {string.Join(",", filters)}" : string.Empty)}" +
                                                $"{(shouldSmelt ? " Excluding Smeltable Files" : string.Empty)}" + '\n' +
        $"                              |   | Files Cannot Be Decompressed: {isDecompressableCount}" + '\n' +
        $"                              |   | Files Locked: {isLockedCount}" + '\n' +
        @"                              |  /");
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
    else if (confirm is 'n')
    {
        Logger.ClearBuffer();
        Environment.Exit(0);
    }
}

Logger.ClearBuffer();

List<Task> tasks = new();
Stopwatch watch = new();
watch.Start();

Logger.NewLine(2);
Logger.LogInfo("################################################################################");
Logger.NewLine(2);
Logger.LogInfo("                          Beginning Extraction Pass...                          ");
Logger.LogInfo("                          This may take a while...                              ");
Logger.NewLine(2);
Logger.LogInfo("################################################################################");
Logger.NewLine(2);
await Task.Delay(TimeSpan.FromSeconds(2));
if (existenceFilteredExtractionEntries.Count > 0)
{
    Parallel.ForEach(existenceFilteredExtractionEntries, entry =>
    {
        FileInfo extractedFile = new(Path.Join(outDirectory.FullName, entry.Name));
        if (!extractedFile.Exists || extractedFile.Length != entry.Size)
        {
            if (detailedLogs)
            {
                Logger.LogInfo($"| - Extracting: {entry.Name}" + '\n' +
                    @"                              |  \" + '\n' +
                    $"                              |   | Date Last Modified: {entry.DateTime}" + '\n' +
                    $"                              |   | Compression Method: {entry.CompressionMethod}" + '\n' +
                    $"                              |   | Compressed Size:   {(float)entry.CompressedSize / 1000000:#,#.######} MB  :  {(float)entry.CompressedSize / 1000000000:#,#.#########} GB" + '\n' +
                    $"                              |   | Uncompressed Size: {(float)entry.Size / 1000000:#,#.######} MB  :  {(float)entry.Size / 1000000000:#,#.#########} GB" + '\n' +
                    @"                              |  /");
            }
            else Logger.LogInfo($"| - Extracting: {entry.Name[(entry.Name.LastIndexOf("/") + 1)..]}");
            if (!extractedFile.Directory.Exists) extractedFile.Directory.Create();
            try
            {
                using FileStream fs = extractedFile.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite); // Dont want people accessing incomplete files.
                using Stream decompStream = pak.GetInputStream(entry);
                StreamUtils.Copy(decompStream, fs, decomBuffer);
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
            catch (AggregateException e)
            {
                Logger.LogException(e);
            }
        }
    });
}
else Logger.LogInfo("No extraction work to be done! Skipping...");
if (existenceFilteredSmeltingEntries.Count > 0)
{
    if (shouldSmelt)
    {
        Logger.NewLine(2);
        Logger.LogInfo("################################################################################");
        Logger.NewLine(2);
        Logger.LogInfo("                            Beginning Smelting Pass...                          ");
        Logger.LogInfo("                            This may take a while...                            ");
        Logger.NewLine(2);
        Logger.LogInfo("################################################################################");
        Logger.NewLine(2);
        await Task.Delay(TimeSpan.FromSeconds(2));
        Parallel.ForEach(existenceFilteredSmeltingEntries, entry =>
        {
            FileInfo extractedFile = new(Path.Join(outDirectory.FullName, entry.Name));
            FileInfo smeltedFile = new(Path.Join(smelterOutDirectory.FullName, entry.Name));
            if (!smeltedFile.Exists)
            {
                Logger.LogInfo($"| - Smelting: {entry.Name}");
                if (!smeltedFile.Directory.Exists) smeltedFile.Directory.Create();
                try
                {
                    if (extractedFile.Extension is ".dcb")
                    {
                        bool legacy = File.OpenRead(extractedFile.FullName).Length < 0x0e2e00; // May be a .NET bug but for some reason FileInfo.Length cannot access the file.
                        using BinaryReader br = new(extractedFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                        new DataForge(br, legacy).Save(Path.ChangeExtension(smeltedFile.FullName, "xml"));
                    }
                    else
                    {
                        XmlDocument xml = CryXmlSerializer.ReadFile(extractedFile.FullName);
                        if (xml != null) xml.Save(Path.ChangeExtension(smeltedFile.FullName, "xml"));
                    }
                }
                catch (ArgumentException e)
                {
                    Logger.LogException(e);
                    // Unsupported file type
                    // TODO: See if we can do anything about the .PeekChar() overflow
                }
                catch (EndOfStreamException e)
                {
                    Logger.LogException(e);
                    // Unsupported file type
                    // TODO: See if we can do anything about the .PeekChar() overflow
                }
            }
        });
    }
}
else Logger.LogInfo("No smelting work to be done! Skipping...");
watch.Stop();

Logger.NewLine(2);
Logger.LogInfo("- Extraction Completed!");
Logger.LogInfo(@" \");
Logger.LogInfo($"  |  Time Taken: {(float)watch.ElapsedMilliseconds / 60000:#,#.###} minutes");
Logger.LogWarn("  |  Due to the nature of SSD's/NVMe's, do not excessively run the extraction on an SSD/NVMe. Doing so may reduce the lifetime of the SSD/NVMe.");
Logger.NewLine(2);
Logger.LogInfo("Would you like to open the output directory? (Application will close on input) y/n: ");
char openOutput = Console.ReadKey().KeyChar;
if (openOutput is 'y') Process.Start("explorer.exe", outDirectory.FullName);

#endregion Program