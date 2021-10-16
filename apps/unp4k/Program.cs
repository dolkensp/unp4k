using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Xml;
using System.Diagnostics;

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

using unforge;
using unlib;

/*
 * TODO: While Linux is supported, we need to add in everything when Star Citizen becomes available on Linux
 */

#region Initialisation

DirectoryInfo? defaultOutputDirectory = new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "unp4k"));
FileInfo? defaultp4kFile = OS.IsWindows ? new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "StarCitizen", "LIVE", "Data.p4k")) :
    new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "unp4k", "Data.p4k"));
DirectoryInfo? defaultExtractionDirectory = new(Path.Join(defaultOutputDirectory.FullName, "output"));

FileInfo? p4kFile = null;
DirectoryInfo? outDirectory = null;
DirectoryInfo? smelterOutDirectory = null;
List<string> filters = new();

bool printErrors = false;
bool detailedLogs = false;
bool shouldSmelt = false;
bool combinePasses = false;
bool forceOverwrite = false;

Logger.ClearBuffer();
Logger.LogInfo("Initialising...");

if (args.Length is 0) 
{
    p4kFile = defaultp4kFile;
    outDirectory = defaultExtractionDirectory;
    filters.Add("*.*");
    Logger.ClearBuffer();
    Logger.LogInfo('\n' +
        "################################################################################" + '\n' + '\n' +
        "                             unp4ck <> Star Citizen                             " + '\n' + '\n' +
        "Extracts Star Citizen's Data.p4k into a directory of choice and even convert them into xml files!" + '\n' + '\n' +
       @"\" + '\n' +
       @" | Windows PowerShell: .\unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' + '\n' +
       @" | Windows Command Prompt: unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' + '\n' +
       @" | Linux Terminal: ./unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' + '\n' +
        " | " + '\n' +
       @" | Windows Example: unp4ck -i " + '"' + @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k" + '"' + " -o " + '"' + @"C:\Windows\SC" + '"' + " -f " + '"' + "*.*" + '"' + " -d" + '\n' +
       @" | Ubuntu Example: unp4ck -i " + '"' + @"/home/USERNAME/unp4k/Data.p4k" + '"' + " -o " + '"' + @"/home/USERNAME/unp4k/output" + '"' + " -f " + '"' + "*.*" + '"' + " -d" + '\n' +
        " | " + '\n' +
       @" |\" + '\n' +
        " | - Mandatory arguments:" + '\n' +
        " | | -i: Delcares the input file path." + '\n' +
        " | | -o: Declared the output directory path." + '\n' +
        " | |" + '\n' +
        " | - Optional arguments:" + '\n' +
        " | | -f: Allows you to filter in the files you want." + '\n' +
        " | | -e: Enables error and exception printing to console." + '\n' +
        " | | -w: Forces all files to be re-extraced and/or re-smelted." + '\n' +
        " | | -c: Makes extraction and smelting run at the same time (requires a lot of RAM)." + '\n' +
        " |/" + '\n' +
        " | " + '\n' +
       @" |\" + '\n' +
        " | - Format Examples:" + '\n' +
        " | | File Type Selection: .dcb" + '\n' +
        " | | Multi-File Type Selection: .dcb,.png,.gif" + '\n' +
        " | | Specific File Selection: Game.dcb" + '\n' +
        " | | Multi-Specific File Selection: Game.dcb,smiley_face.png,its_working.gif" + '\n' +
        " |/" + '\n' +
        "/" + '\n' +
        "################################################################################" + '\n' + '\n' +
       $"NO INPUT Data.p4k PATH HAS BEEN DECLARED. USING DEFAULT PATH " + '"' + $"{defaultp4kFile.FullName}" + '"' + '\n' +
        "NO OUTPUT DIRECTORY PATH HAS BEEN DECLARED. ALL EXTRACTS WILL GO INTO " + '"' + $"{defaultExtractionDirectory.FullName}" + '"' + '\n' + '\n' +
        "Press any key to continue!");
    Console.ReadKey();
    Logger.ClearBuffer();
}

if (OS.IsLinux)
{
    char? proceedAsRoot = null;
    while (proceedAsRoot is null)
    {
        Logger.LogWarn("unp4k has been run as root via the sudo command.");
        Logger.LogWarn("This may cause issues because it will make the app target the /root/ path!");
        Logger.NewLine();
        Logger.LogInfo("Are you sure you want to proceed? y/n: ");
        proceedAsRoot = Console.ReadKey().KeyChar;
        if (proceedAsRoot is null || proceedAsRoot != 'y' && proceedAsRoot != 'n')
        {
            Logger.LogError("Please input y for yes or n for no!");
            await Task.Delay(TimeSpan.FromSeconds(3));
            Logger.ClearBuffer();
            proceedAsRoot = null;
        }
        else if (proceedAsRoot is 'n')
        {
            Logger.ClearBuffer();
            Environment.Exit(0);
        }
    }
}

try
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i].ToLowerInvariant() is "-i") p4kFile = new(args[i + 1]);
        else if (args[i].ToLowerInvariant() is "-o") outDirectory = new(args[i + 1]);
        else if (args[i].ToLowerInvariant() is "-f") filters = args[i + 1].Split(',').ToList();
        else if (args[i].ToLowerInvariant() is "-e") printErrors = true;
        else if (args[i].ToLowerInvariant() is "-d") detailedLogs = true;
        else if (args[i].ToLowerInvariant() is "-c") combinePasses = true;
        else if (args[i].ToLowerInvariant() is "-w") forceOverwrite = true;
        else if (args[i].ToLowerInvariant() is "-forge") shouldSmelt = true;
    }
}
catch (IndexOutOfRangeException e)
{
    if (printErrors) Logger.LogException(e);
    else Logger.LogError("An error has occured with the argument parser. Please ensure you have provided the relevant arguments!");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

if (p4kFile is null) p4kFile = defaultp4kFile;
if (outDirectory is null) outDirectory = defaultExtractionDirectory;
if (smelterOutDirectory is null) smelterOutDirectory = new(Path.Join(outDirectory.FullName, "Smelted"));
if (filters.Count is 0) filters.Add("*.*");

if (!p4kFile.Exists)
{
    Logger.LogError($"Input path '{p4kFile.FullName}' does not exist!");
    Logger.LogError($"Make sure you have the path pointing to a Star Citizen Data.p4k file!");
    Console.ReadKey();
    Logger.ClearBuffer();
    Environment.Exit(0);
}

if (!outDirectory.Exists) outDirectory.Create();
if (!smelterOutDirectory.Exists) smelterOutDirectory.Create();

#endregion

#region Program

Console.Title = $"unp4k: Processing {p4kFile.FullName}";
Logger.ClearBuffer();
Logger.LogInfo($"[0% Complete] Processing Data.p4k before extraction{(shouldSmelt ? " and smelting" : string.Empty)}, this may take a while...");

using FileStream p4kStream = p4kFile.Open(FileMode.Open, FileAccess.Read, FileShare.None); // The Data.p4k must be locked while it is being read to avoid corruption.
ZipFile pak = new(p4kStream);
pak.KeysRequired += (object sender, KeysRequiredEventArgs e) => e.Key = new byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };

byte[] decomBuffer = new byte[4096];
ConcurrentQueue<ZipEntry> filteredEntries = new();
ConcurrentQueue<ZipEntry> existenceFilteredExtractionEntries = new();
ConcurrentQueue<ZipEntry> existenceFilteredSmeltingEntries = new();

bool additionalFiles = false;
int isDecompressableCount = 0;
int isLockedCount = 0;
long bytesSize = 0L;
foreach (ZipEntry entry in pak) filteredEntries.Enqueue(entry);
Logger.ClearBuffer();
Logger.LogInfo($"[{(shouldSmelt ? "25" : "33")}% Complete] Processing Data.p4k before extraction{(shouldSmelt ? " and smelting" : string.Empty)}, this may take a while...");
Logger.LogInfo("Testing Data.p4k Entry Integrity...");
filteredEntries = new(filteredEntries.Where(x => filters.Contains("*.*") ? true : filters.Any(o => x.Name.Contains(o))).Where(x =>
{
    bool isDecompressable = x.CanDecompress;
    bool isLocked = x.IsCrypted || x.IsAesCrypted;
    if (isDecompressable) isDecompressableCount++;
    if (isLocked) isLockedCount++;
    return isDecompressable && !isLocked;
}).OrderBy(x => x.Name));
Logger.ClearBuffer();
Logger.LogInfo($"[{(shouldSmelt ? "50" : "66")}% Complete] Processing Data.p4k before extraction{(shouldSmelt ? " and smelting" : string.Empty)}, this may take a while...");
Logger.LogInfo("Optimising Extractable File List...");
existenceFilteredExtractionEntries = new(filteredEntries.Where(x => 
{
    FileInfo f = new(Path.Join(outDirectory.FullName, x.Name));
    return forceOverwrite || !f.Exists || f.Length != x.Size;
}));
if (shouldSmelt)
{
    Logger.ClearBuffer();
    Logger.LogInfo($"[75% Complete] Processing Data.p4k before extraction{(shouldSmelt ? " and smelting" : string.Empty)}, this may take a while...");
    Logger.LogInfo("Optimising Smeltable File List...");
    existenceFilteredSmeltingEntries = new(filteredEntries.Where(x => 
    {
        FileInfo f = new(Path.ChangeExtension(Path.Join(smelterOutDirectory.FullName, x.Name), "xml"));
        return forceOverwrite || !f.Exists || f.Length != x.Size;
    }));
}

Logger.ClearBuffer();
DriveInfo outputDrive = DriveInfo.GetDrives().First(x => OS.IsWindows ? x.Name == outDirectory.FullName[..3] : new DirectoryInfo(x.Name).Exists);
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

char? goAheadWithExtraction = null;
while (goAheadWithExtraction is null)
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
    goAheadWithExtraction = Console.ReadKey().KeyChar;
    if (goAheadWithExtraction is null || goAheadWithExtraction != 'y' && goAheadWithExtraction != 'n')
    {
        Logger.LogError("Please input y for yes or n for no!");
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

Logger.ClearBuffer();

Stopwatch watch = new();
watch.Start();

Logger.NewLine(2);
Logger.LogInfo("##########  Beginning Extraction Pass...  ##########");
Logger.LogInfo("##########  This may take a while...  ##########");
Logger.NewLine(2);
await Task.Delay(TimeSpan.FromSeconds(2));
if (existenceFilteredExtractionEntries.Count > 0)
{
    int tasksCompleted = 0;
    Parallel.ForEach(existenceFilteredExtractionEntries, entry =>
    {
        FileInfo extractedFile = new(Path.Join(outDirectory.FullName, entry.Name));
        string percentage = tasksCompleted is 0 ? 0F.ToString() : (100F * (float)tasksCompleted / existenceFilteredExtractionEntries.Count).ToString("#,#.###");
        if (detailedLogs)
        {
            Logger.LogInfo($"| [{percentage}%] - Extracting" +
                                                                                                $"{(combinePasses ? " & Smelting" : string.Empty)}: {entry.Name}" + '\n' +
                @"                              |  \" + '\n' +
                $"                              |   | Date Last Modified: {entry.DateTime}" + '\n' +
                $"                              |   | Compression Method: {entry.CompressionMethod}" + '\n' +
                $"                              |   | Compressed Size:   {(float)entry.CompressedSize / 1000000:#,#.######} MB  :  {(float)entry.CompressedSize / 1000000000:#,#.#########} GB" + '\n' +
                $"                              |   | Uncompressed Size: {(float)entry.Size / 1000000:#,#.######} MB  :  {(float)entry.Size / 1000000000:#,#.#########} GB" + '\n' +
                @"                              |  /");
        }
        else Logger.LogInfo($"| [{percentage}%] - Extracting: {entry.Name[(entry.Name.LastIndexOf("/") + 1)..]}");
        if (!extractedFile.Directory.Exists) extractedFile.Directory.Create();
        try
        {
            using FileStream fs = extractedFile.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite); // Dont want people accessing incomplete files.
            using Stream decompStream = pak.GetInputStream(entry);
            StreamUtils.Copy(decompStream, fs, decomBuffer);
            if (combinePasses) Smelt(extractedFile, new(Path.Join(smelterOutDirectory.FullName, entry.Name)));
        }
        catch (DirectoryNotFoundException e)
        {
            if (printErrors) Logger.LogException(e);
        }
        catch (FileNotFoundException e)
        {
            if (printErrors) Logger.LogException(e);
        }
        catch (IOException e)
        {
            if (printErrors) Logger.LogException(e);
        }
        catch (AggregateException e)
        {
            if (printErrors) Logger.LogException(e);
        }
        finally
        {
            tasksCompleted++;
        }
    });
}
else Logger.LogInfo("No extraction work to be done! Skipping...");
if (existenceFilteredSmeltingEntries.Count > 0)
{
    if (shouldSmelt && !combinePasses)
    {
        Logger.NewLine(2);
        Logger.LogInfo("##########  Beginning Smelting Pass...  ##########");
        Logger.LogInfo("##########  This may take a while...  ##########");
        Logger.NewLine(2);
        await Task.Delay(TimeSpan.FromSeconds(2));
        int tasksCompleted = 0;
        Parallel.ForEach(existenceFilteredSmeltingEntries, entry =>
        {
            Logger.LogInfo($"| [{(tasksCompleted is 0 ? 0F.ToString() : (100F * (float)tasksCompleted / existenceFilteredExtractionEntries.Count).ToString("#,#.###"))}%] - Smelting: {entry.Name}");
            Smelt(new(Path.Join(outDirectory.FullName, entry.Name)), new(Path.Join(smelterOutDirectory.FullName, entry.Name)));
            tasksCompleted++;
        });
    }
}
else Logger.LogInfo("No smelting work to be done! Skipping...");

void Smelt(FileInfo extractedFile, FileInfo smeltedFile)
{
    if (!smeltedFile.Directory.Exists) smeltedFile.Directory.Create();
    try
    {
        if (extractedFile.Extension is ".dcb") new DataForge(extractedFile).Save(smeltedFile);
        else new CryXmlSerializer(extractedFile).Save(smeltedFile);
    }
    catch (ArgumentException e)
    {
        if (printErrors) Logger.LogException(e);
        // Unsupported file type
        // TODO: See if we can do anything about the .PeekChar() overflow
    }
    catch (EndOfStreamException e)
    {
        if (printErrors) Logger.LogException(e);
        // Unsupported file type
        // TODO: See if we can do anything about the .PeekChar() overflow
    }
    catch (DirectoryNotFoundException e)
    {
        if (printErrors) Logger.LogException(e);
    }
    catch (FileNotFoundException e)
    {
        if (printErrors) Logger.LogException(e);
    }
    catch (IOException e)
    {
        if (printErrors) Logger.LogException(e);
    }
}

watch.Stop();

Logger.NewLine(2);
Logger.LogInfo("- Extraction Completed!");
Logger.LogInfo(@" \");
Logger.LogInfo($"  |  Time Taken: {(float)watch.ElapsedMilliseconds / 60000:#,#.###} minutes");
Logger.LogWarn("  |  Due to the nature of SSD's/NVMe's, do not excessively run the extraction on an SSD/NVMe. Doing so may reduce the lifetime of the SSD/NVMe.");
Logger.NewLine(2);
Logger.LogInfo("Would you like to open the output directory? (Application will close on input) y/n: ");
char openOutput = Console.ReadKey().KeyChar;
if (openOutput is 'y') Process.Start(OS.IsWindows ? "explorer" : "nautilus", outDirectory.FullName);

#endregion Program