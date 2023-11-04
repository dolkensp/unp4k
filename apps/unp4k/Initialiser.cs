using unlib;

namespace unp4k;

internal static class Initialiser
{
    // CIG seemingly do not store any record of where Star Citizen is installed in any parsable format due to the launcher being Chromium based.
    private static DirectoryInfo? DefaultOutputDirectory { get; } = new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "unp4k"));
    private static DirectoryInfo? DefaultExtractionDirectory { get; } = new(Path.Join(DefaultOutputDirectory.FullName, "output"));
    private static FileInfo? Defaultp4kFile { get; } = OS.IsWindows ?
        new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "StarCitizen", "LIVE", "Data.p4k")) :
        new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "unp4k", "Data.p4k"));

    private static readonly string Manual = 
                "Extracts Star Citizen's Data.p4k into a directory of choice and optionally converts them into standard XML or JSON formats!" + '\n' + '\n' +
               @"\" + '\n' +
                " | Repository: https://github.com/dolkensp/unp4k" + '\n' +
               @" |\" + '\n' +
                " | - Required Arguments:" + '\n' +
                " | | -i or -input: The input file path." + '\n' +
                " | | -o or -output: The output directory path." + '\n' +
                " | |" + '\n' +
                " | - Optional Arguments:" + '\n' +
                " | | -f   or --filter:    Allows you to filter in the files you want." + '\n' +
                " | | -d   or --details:   Enabled detailed logging including errors." + '\n' +
                " | | -unf or --unforge:   Enables unforge to forge extracted files." + '\n' +
                " | | -j   or --json:      Converts all CryXML to JSON while retaining standard XML files." + '\n' +
                " | | -ow  or --overwrite: Overwrites files that already exist." + '\n' +
                " | | -y   or --accept:    Don't ask for input, just continue. Recommended for automated systems." + '\n' +
                " |/" + '\n' +
                "/" + '\n';

    internal static void PreInit()
    {
        Logger.ClearBuffer();
        Logger.SetTitle($"unp4k: Pre-Initializing...");

        // Parse the arguments and do what they represent
        for (int i = 0; i < Globals.Arguments.Count; i++)
        {
            if (Globals.Arguments[i].ToLowerInvariant() is "-i"   || Globals.Arguments[i].ToLowerInvariant() is "--input")          Globals.P4kFile                 = new(Globals.Arguments[i + 1]);
            else if (Globals.Arguments[i].ToLowerInvariant() is "-o"   || Globals.Arguments[i].ToLowerInvariant() is "--output")    Globals.OutDirectory            = new(Globals.Arguments[i + 1]);
            else if (Globals.Arguments[i].ToLowerInvariant() is "-t"   || Globals.Arguments[i].ToLowerInvariant() is "--threads") 
            {
                if (int.TryParse(Globals.Arguments[i + 1], out int threads))                                                        Globals.ThreadLimit             = threads;
                else throw new InvalidCastException(Globals.Arguments[i + 1]);
            }
            else if (Globals.Arguments[i].ToLowerInvariant() is "-f"   || Globals.Arguments[i].ToLowerInvariant() is "--filter")    Globals.Filters                 = [.. Globals.Arguments[i + 1].Split(',')];
            else if (Globals.Arguments[i].ToLowerInvariant() is "-d"   || Globals.Arguments[i].ToLowerInvariant() is "--details")   Globals.ShouldPrintDetailedLogs = true;
            else if (Globals.Arguments[i].ToLowerInvariant() is "-unf" || Globals.Arguments[i].ToLowerInvariant() is "--unforge")   Globals.ShouldUnForge           = true;
            else if (Globals.Arguments[i].ToLowerInvariant() is "-j"   || Globals.Arguments[i].ToLowerInvariant() is "--json")      Globals.ShouldConvertToJson     = true;
            else if (Globals.Arguments[i].ToLowerInvariant() is "-ow"  || Globals.Arguments[i].ToLowerInvariant() is "--overwrite") Globals.ShouldOverwrite         = true;
            else if (Globals.Arguments[i].ToLowerInvariant() is "-y"   || Globals.Arguments[i].ToLowerInvariant() is "--accept")    Globals.ShouldAcceptEverything  = true;
        }

        bool hasInput = false;
        bool hasOutput = false;
        if (!(hasInput = Globals.P4kFile != null) || !(hasOutput = Globals.OutDirectory != null))
        {
            if (!hasInput) Globals.P4kFile = Defaultp4kFile;
            if (!hasOutput) Globals.OutDirectory = DefaultExtractionDirectory;

            // Basically show the user the manual if there are missing arguments.
            Logger.Write($"{Manual}{(!hasInput ? $"\nNO INPUT Data.p4k PATH HAS BEEN DECLARED. USING DEFAULT PATH {Defaultp4kFile.FullName}" : string.Empty)}" + // TODO: See if we can get the install path in registry.
                $"{(!hasOutput ? $"\nNO OUTPUT DIRECTORY PATH HAS BEEN DECLARED. ALL EXTRACTS WILL GO INTO {DefaultExtractionDirectory.FullName}" : string.Empty)}" +
                "\n\nPress any key to continue!\n");
            Console.ReadKey();
            Logger.ClearBuffer();
        }
    }

    internal static void Init()
    {
        Logger.SetTitle($"unp4k: Initializing...");
        // Default any of the null argument declared variables.
        Globals.P4kFile ??= Defaultp4kFile;
        Globals.OutDirectory ??= DefaultExtractionDirectory;
        Globals.OutForgedDirectory ??= new(Path.Join(Globals.OutDirectory.FullName, "Forged"));
        if (!Globals.P4kFile.Exists)
        {
            Logger.LogError($"Input path '{Globals.P4kFile.FullName}' does not exist!");
            Logger.LogError($"Make sure you have the path pointing to a Star Citizen Data.p4k file!");
            if (!Globals.ShouldAcceptEverything) Console.ReadKey();
            Globals.InternalExitTrigger = true;
            return;
        }
        if (!Globals.OutDirectory.Exists) Globals.OutDirectory.Create();
        if (!Globals.OutForgedDirectory.Exists) Globals.OutForgedDirectory.Create();
    }

    internal static void PostInit()
    {
        Logger.SetTitle($"unp4k: Post-Initializing...");
        if (!Globals.ShouldAcceptEverything)
        {
            // Show the user any warning if anything worrisome is detected.
            bool newLineCheck = false;
            if (OS.IsLinux && Environment.UserName.Equals("root", StringComparison.CurrentCultureIgnoreCase))
            {
                newLineCheck = true;
                Logger.NewLine();
                Logger.LogWarn("LINUX ROOT WARNING:" + '\n' +
                    "unp4k has been run as root via the sudo command!" + '\n' +
                    "This may cause issues due to the home path being '/root/'!");
            }
            if (Globals.Filters.Contains("*.*"))
            {
                if (newLineCheck) Logger.NewLine();
                else newLineCheck = true;
                Logger.LogWarn("ENORMOUS JOB WARNING:" + '\n' + 
                    "unp4k has been run with no filters or the *.* filter!" + '\n' +
                    $"When extracted{(Globals.ShouldUnForge ? " and forged" : string.Empty)}, it will take up a lot of storage space and queues 100,000's of tasks in the process.");
            }
            if (Globals.ShouldOverwrite)
            {
                if (newLineCheck) Logger.NewLine();
                else newLineCheck = true;
                Logger.LogWarn("OVERWRITE ENABLED:" + '\n' +
                    "unp4k has been run with the overwrite option!" + '\n' +
                    "Overwriting files will potentially take much longer than choosing a new empty directory!");
            }
            if (!Logger.AskUserInput("Proceed?"))
            {
                Globals.InternalExitTrigger = true;
                return;
            }
        }
    }
}