using unlib;

namespace unp4k;
internal static class Initialiser
{
    private static DirectoryInfo? DefaultOutputDirectory { get; }  = new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "unp4k"));
    private static FileInfo? Defaultp4kFile { get; } = OS.IsWindows ? new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "StarCitizen", "LIVE", "Data.p4k")) :
        new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "unp4k", "Data.p4k"));
    private static DirectoryInfo? DefaultExtractionDirectory { get; } = new(Path.Join(DefaultOutputDirectory.FullName, "output"));
    private static string Banner = '\n' +
                "################################################################################" + '\n' + '\n' +
                "                              unp4k <> Star Citizen                             " + '\n' + '\n' +
                "################################################################################" + '\n' + '\n';

    internal static void PreInit()
    {
        Logger.ClearBuffer();
        Console.Title = $"unp4k: Pre-Initializing...";

        if (Globals.Arguments.Count is 0)
        {
            Globals.P4kFile = Defaultp4kFile;
            Globals.OutDirectory = DefaultExtractionDirectory;
            Globals.Filters.Add("*.*");
            // Basically show the user the manual if there are no arguments.
            Console.Write('\n' +
                "################################################################################" + '\n' + '\n' +
                "                              unp4k <> Star Citizen                             " + '\n' + '\n' +
                "Extracts Star Citizen's Data.p4k into a directory of choice and even convert them into xml files!" + '\n' + '\n' +
               @"\" + '\n' +
               @" | Windows PowerShell: .\unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' + '\n' +
               @" | Windows Command Prompt: unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' + '\n' +
               @" | Linux Terminal: ./unp4ck -d -i " + '"' + "[InFilePath]" + '"' + " -o " + '"' + "[OutDirectoryPath]" + '"' + '\n' +
                " | " + '\n' +
               @" | Windows Example: unp4ck -i " + '"' + @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k" + '"' + " -o " + 
                        '"' + @"C:\Windows\SC" + '"' + " -f " + '"' + "*.*" + '"' + " -d" + '\n' +
               @" | Ubuntu Example: unp4ck -i " + '"' + @"/home/USERNAME/unp4k/Data.p4k" + '"' + " -o " + '"' + @"/home/USERNAME/unp4k/output" + 
                        '"' + " -f " + '"' + "*.*" + '"' + " -d" + '\n' +
                " | " + '\n' +
               @" |\" + '\n' +
                " | - Mandatory arguments:" + '\n' +
                " | | -i: Delcares the input file path." + '\n' +
                " | | -o: Declared the output directory path." + '\n' +
                " | |" + '\n' +
                " | - Optional arguments:" + '\n' +
                " | | -f: Allows you to filter in the files you want." + '\n' +
                " | | -e: Enables error and exception printing to console." + '\n' +
                " | | -l: Enabled detailed logging." + '\n' +
                " | | -c: Combines both extraction passes into one (can require, in some cases 16GB+ of RAM/Pagefile)." + '\n' +
                " | | -w: Forces all files to be re-extraced." + '\n' +
                " | | -d: Deletes the output directory if it already exists on start." + '\n' +
                " | | -forge: Enables unforge to forge extracted files." + '\n' +
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
               $"NO INPUT Data.p4k PATH HAS BEEN DECLARED. USING DEFAULT PATH " + '"' + $"{Defaultp4kFile.FullName}" + '"' + '\n' +
                "NO OUTPUT DIRECTORY PATH HAS BEEN DECLARED. ALL EXTRACTS WILL GO INTO " + '"' + $"{DefaultExtractionDirectory.FullName}" + '"'
                + '\n' + '\n' + "Press any key to continue!" + '\n');
            Console.ReadKey();
            Logger.ClearBuffer();
        }

        // Parse the arguments and do what they represent
        try
        {
            for (int i = 0; i < Globals.Arguments.Count; i++)
            {
                if      (Globals.Arguments[i].ToLowerInvariant() is "-i")       Globals.P4kFile =       new(Globals.Arguments[i + 1]);
                else if (Globals.Arguments[i].ToLowerInvariant() is "-o")       Globals.OutDirectory =  new(Globals.Arguments[i + 1]);
                else if (Globals.Arguments[i].ToLowerInvariant() is "-f")       Globals.Filters =       Globals.Arguments[i + 1].Split(',').ToList();
                else if (Globals.Arguments[i].ToLowerInvariant() is "-e")       Globals.PrintErrors     = true;
                else if (Globals.Arguments[i].ToLowerInvariant() is "-l")       Globals.DetailedLogs    = true;
                else if (Globals.Arguments[i].ToLowerInvariant() is "-c")       Globals.CombinePasses   = true;
                else if (Globals.Arguments[i].ToLowerInvariant() is "-w")       Globals.ForceOverwrite  = true;
                else if (Globals.Arguments[i].ToLowerInvariant() is "-d")       Globals.DeleteOutput    = true;
                else if (Globals.Arguments[i].ToLowerInvariant() is "-forge")   Globals.ShouldSmelt     = true;
            }
        }
        catch (IndexOutOfRangeException e)
        {
            if (Globals.PrintErrors) Logger.LogException(e);
            else Logger.LogError("An error has occured with the argument parser. Please ensure you have provided the relevant arguments!");
            Console.ReadKey();
            Globals.ExitTrigger = true;
            return;
        }
    }

    internal static void Init()
    {
        Console.Title = $"unp4k: Initializing...";

        // Default any of the null argument declared variables.
        if (Globals.P4kFile is null) Globals.P4kFile = Defaultp4kFile;
        if (Globals.OutDirectory is null) Globals.OutDirectory = DefaultExtractionDirectory;
        if (Globals.SmelterOutDirectory is null) Globals.SmelterOutDirectory = new(Path.Join(Globals.OutDirectory.FullName, "Smelted"));
        if (Globals.Filters.Count is 0) Globals.Filters.Add("*.*");

        if (!Globals.P4kFile.Exists)
        {
            Logger.LogError($"Input path '{Globals.P4kFile.FullName}' does not exist!");
            Logger.LogError($"Make sure you have the path pointing to a Star Citizen Data.p4k file!");
            Console.ReadKey();
            Globals.ExitTrigger = true;
            return;
        }

        if (!Globals.OutDirectory.Exists) Globals.OutDirectory.Create();
        if (!Globals.SmelterOutDirectory.Exists) Globals.SmelterOutDirectory.Create();
    }

    internal static async Task PostInit()
    {
        Console.Title = $"unp4k: Post-Initializing...";

        // Show the user any warning if anything worrisome is detected.
        char? proceed = null;
        bool shouldCheckProceed = false;
        while (proceed is null)
        {
            Console.Write(Banner);
            if (OS.IsLinux && Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Contains("/root/"))
            {
                shouldCheckProceed = true;
                Logger.NewLine();
                Logger.LogWarn("LINUX ROOT WARNING:");
                Logger.LogWarn("unp4k has been run as root via the sudo command!");
                Logger.LogWarn("This may cause issues because it will make the app target the /root/ path!");
            }
            if (Globals.Filters.Contains("*.*") || Globals.Filters.Any(x => x.Contains(".dcb")))
            {
                if (shouldCheckProceed) Logger.NewLine();
                else shouldCheckProceed = true;
                Logger.LogWarn("ENORMOUS JOB WARNING:");
                Logger.LogWarn("unp4k has been run with filters which include Star Citizen's Game.dcb file!");
                Logger.LogWarn("Due to what the Game.dcb contains, unp4k will need to run for far longer and will requires possibly hundreds of gigabytes of free space!");
            }
            if (Globals.ForceOverwrite)
            {
                if (shouldCheckProceed) Logger.NewLine();
                else shouldCheckProceed = true;
                Logger.LogWarn("OVERWRITE ENABLED:");
                Logger.LogWarn("unp4k has been run with the overwrite option!");
                Logger.LogWarn("Overwriting files could take very long depending on your other options!");
            }
            if (Globals.DeleteOutput)
            {
                if (shouldCheckProceed) Logger.NewLine();
                else shouldCheckProceed = true;
                Logger.LogWarn("DELETE OUTPUT ENABLED:");
                Logger.LogWarn($"unp4k will delete {Globals.OutDirectory}");
                Logger.LogWarn("This could take a while depending on your storage drives Random 4K read/write speed and depending on how many files which have already been extracted!");
            }
            if (shouldCheckProceed)
            {
                Logger.NewLine();
                Console.Write("Are you sure you want to proceed? y/n: ");
                proceed = Console.ReadKey().KeyChar;
                if (proceed is null || proceed != 'y' && proceed != 'n')
                {
                    Console.Error.WriteLine("Please input y for yes or n for no!");
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    Logger.ClearBuffer();
                    proceed = null;
                }
                else if (proceed is 'n')
                {
                    Globals.ExitTrigger = true;
                    return;
                }
            }
            else break;
        }
        Logger.NewLine(2);
    }
}
