using unlib;

namespace unp4k;

internal static class Initialiser
{
    private static DirectoryInfo? DefaultOutputDirectory { get; } = new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "unp4k"));
    private static DirectoryInfo? DefaultExtractionDirectory { get; } = new(Path.Join(DefaultOutputDirectory.FullName, "output"));
    private static FileInfo? Defaultp4kFile { get; } = OS.IsWindows ?
        new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "StarCitizen", "LIVE", "Data.p4k")) :
        new(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "unp4k", "Data.p4k"));

    private static readonly string Manual = 
                "Extracts Star Citizen's Data.p4k into a directory of choice and optionally converts them into standard xml files!" + '\n' + '\n' +
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
                " | - Required Arguments:" + '\n' +
                " | | -i or -input: The input file path." + '\n' +
                " | | -o or -output: The output directory path." + '\n' +
                " | |" + '\n' +
                " | - Optional Arguments:" + '\n' +
                " | | -f or -filter: Allows you to filter in the files you want." + '\n' +
                " | | -e or -errors: Enables error and exception printing to console." + '\n' +
                " | | -d or -details: Enabled detailed logging." + '\n' +
                " | | -w or -overwrite: Forces all files to be re-extracted/re-forged." + '\n' +
                " | | -d or -details: Deletes the output directory if it already exists on start." + '\n' +
                " | | -f or -forge: Enables unforge to forge extracted files." + '\n' +
                " | | -y or -accept: Don't ask for input, just continue. Recommended for automated systems." + '\n' +
                " | | -g or -git: Opens unp4k's GitHub repository in your default browser" + '\n' +
                " |/" + '\n' +
                " | " + '\n' +
               @" |\" + '\n' +
                " | - Filter Formatting Examples:" + '\n' +
                " | | File Type Selection: .dcb" + '\n' +
                " | | Multi-File Type Selection: .dcb,.png,.gif" + '\n' +
                " | | Specific File Selection: Game.dcb" + '\n' +
                " | | Multi-Specific File Selection: Game.dcb,smiley_face.png,its_working.gif" + '\n' +
                " |/" + '\n' +
                " | " + '\n' +
               @" |\" + '\n' +
                " | - What is the unforge?" + '\n' +
                " | | The unforge is a ways and means of deserialising CryXML, which is CryEngine's unique XML syntax." + '\n' +
                " | | This is basically converting CryXML syntax to standard XML syntax." + '\n' +
                " |/" + '\n' +
                "/" + '\n';

    internal static void PreInit()
    {
        Logger.ClearBuffer();
        Logger.SetTitle($"unp4k: Pre-Initializing...");
        if (Globals.Arguments.Count is 0 or 1)
        {
            bool hasInput =  Globals.Arguments.Contains("-i") || Globals.Arguments.Contains("-input");
            bool hasOutput = Globals.Arguments.Contains("-o") || Globals.Arguments.Contains("-output");
            bool hasFilter = Globals.Arguments.Contains("-f") || Globals.Arguments.Contains("-filter");
            if (!hasInput)  Globals.P4kFile = Defaultp4kFile;
            if (!hasOutput) Globals.OutDirectory = DefaultExtractionDirectory;
            if (!hasFilter) Globals.Filters.Add("*.*");
            // Basically show the user the manual if there are missing arguments.
            Logger.Write($"{Manual}{(!hasInput ? $"\nNO INPUT Data.p4k PATH HAS BEEN DECLARED. USING DEFAULT PATH {Defaultp4kFile.FullName}" : string.Empty)}" + // TODO: See if we can get the install path in registry.
                $"{(!hasOutput ? $"\nNO OUTPUT DIRECTORY PATH HAS BEEN DECLARED. ALL EXTRACTS WILL GO INTO {DefaultExtractionDirectory.FullName}" : string.Empty)}" +
                $"{(!hasFilter ? $"\nNO FILTER HAS BEEN DECLARED. USING NO FILTER!" : string.Empty)}" +
                "\n\nPress any key to continue!\n");
            Console.ReadKey();
            Logger.ClearBuffer();
        }

        // Parse the arguments and do what they represent
        for (int i = 0; i < Globals.Arguments.Count; i++)
        {
            if (Globals.Arguments[i].ToLowerInvariant() is "-i" || Globals.Arguments[i].ToLowerInvariant() is "-input")             Globals.P4kFile                     = new(Globals.Arguments[i + 1]);
            else if (Globals.Arguments[i].ToLowerInvariant() is "-o" || Globals.Arguments[i].ToLowerInvariant() is "-output")       Globals.OutDirectory                = new(Globals.Arguments[i + 1]);
            else if (Globals.Arguments[i].ToLowerInvariant() is "-f" || Globals.Arguments[i].ToLowerInvariant() is "-filter")       Globals.Filters                     = [.. Globals.Arguments[i + 1].Split(',')];
            else if (Globals.Arguments[i].ToLowerInvariant() is "-e" || Globals.Arguments[i].ToLowerInvariant() is "-errors")       Globals.ShouldPrintErrors           = true;
            else if (Globals.Arguments[i].ToLowerInvariant() is "-d" || Globals.Arguments[i].ToLowerInvariant() is "-details")      Globals.ShouldPrintDetailedLogs     = true;
            else if (Globals.Arguments[i].ToLowerInvariant() is "-w" || Globals.Arguments[i].ToLowerInvariant() is "-overwrite")    Globals.ShouldOverwrite             = true;
            else if (Globals.Arguments[i].ToLowerInvariant() is "-p" || Globals.Arguments[i].ToLowerInvariant() is "-delput")       Globals.ShouldDeleteOutput          = true;
            else if (Globals.Arguments[i].ToLowerInvariant() is "-s" || Globals.Arguments[i].ToLowerInvariant() is "-forge")        Globals.ShouldForge                 = true;
            else if (Globals.Arguments[i].ToLowerInvariant() is "-y" || Globals.Arguments[i].ToLowerInvariant() is "-accept")       Globals.ShouldAcceptEverything      = true;
            else if (Globals.Arguments[i].ToLowerInvariant() is "-g" || Globals.Arguments[i].ToLowerInvariant() is "-git")          Platform.OpenWebpage(new Uri("https://github.com/dolkensp/unp4k"));
        }
    }

    internal static void Init()
    {
        Logger.SetTitle($"unp4k: Initializing...");
        // Default any of the null argument declared variables.
        Globals.P4kFile ??= Defaultp4kFile;
        Globals.OutDirectory ??= DefaultExtractionDirectory;
        Globals.OutForgedDirectory ??= new(Path.Join(Globals.OutDirectory.FullName, "Forged"));
        if (Globals.Filters.Count is 0) Globals.Filters.Add("*.*");
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
                    $"When extracted{(Globals.ShouldForge ? " and forged" : string.Empty)}, it will take up a lot of storage space and queues 100,000's of tasks in the process.");
            }
            if (Globals.ShouldOverwrite)
            {
                if (newLineCheck) Logger.NewLine();
                else newLineCheck = true;
                Logger.LogWarn("OVERWRITE ENABLED:" + '\n' +
                    "unp4k has been run with the overwrite option!" + '\n' +
                    "Overwriting files will potentially take much longer than choosing a new empty directory!");
            }
            if (Globals.ShouldDeleteOutput)
            {
                if (newLineCheck) Logger.NewLine();
                Logger.LogWarn("DELETE OUTPUT ENABLED:" + '\n' +
                    $"unp4k will delete {Globals.OutDirectory}" + '\n' +
                    "This may a few minutes longer but will benefit the extraction speed!");
            }
            if (!Logger.AskUserInput("Proceed?"))
            {
                Globals.InternalExitTrigger = true;
                return;
            }
        }
    }
}