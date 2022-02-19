using unlib;

namespace unp4k;
internal static class Globals
{
    internal static FileInfo? p4kFile = null;
    internal static DirectoryInfo? outDirectory = null;
    internal static DirectoryInfo? smelterOutDirectory = null;
    internal static List<string> filters = new();

    internal static bool printErrors = false;
    internal static bool detailedLogs = false;
    internal static bool shouldSmelt = false;
    internal static bool combinePasses = false;
    internal static bool forceOverwrite = false;
}
