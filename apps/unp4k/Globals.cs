namespace unp4k;
internal static class Globals
{
    internal static FileInfo? P4kFile = null;
    internal static DirectoryInfo? OutDirectory = null;
    internal static DirectoryInfo? SmelterOutDirectory = null;
    internal static List<string> Filters = new();

    internal static bool ExitTrigger = false;
    internal static bool PrintErrors = false;
    internal static bool DetailedLogs = false;
    internal static bool ShouldSmelt = false;
    internal static bool CombinePasses = false;
    internal static bool ForceOverwrite = false;
    internal static bool DeleteOutput = false;
}
