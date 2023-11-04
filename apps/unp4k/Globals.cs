namespace unp4k;

internal static class Globals
{
    internal static List<string> Arguments = null;

    internal static FileInfo? P4kFile = null;
    internal static DirectoryInfo? OutDirectory = null;
    internal static DirectoryInfo? OutForgedDirectory = null;
    internal static List<string> Filters = [];

    internal static bool InternalExitTrigger = false;

    internal static bool ShouldPrintDetailedLogs = false;
    internal static bool ShouldUnForge = false;
    internal static bool ShouldConvertToJson = false;
    internal static bool ShouldOverwrite = false;
    internal static bool ShouldAcceptEverything = false;

    internal static int ThreadLimit = Environment.ProcessorCount;
    internal static int FileErrors = 0;
}
