namespace unp4k;

public static class Globals
{
    public static List<string>? Arguments = null;

    public static FileInfo? P4kFile { get; internal set; } = null;
    public static DirectoryInfo? OutDirectory { get; internal set; } = null;
    public static DirectoryInfo? OutForgedDirectory { get; internal set; } = null;
    public static List<string> Filters { get; internal set; } = [];

    public static bool ShouldPrintDetailedLogs { get; internal set; } = false;
    public static bool ShouldUnForge { get; internal set; } = false;
    public static bool ShouldConvertToJson { get; internal set; } = false;
    public static bool ShouldOverwrite { get; internal set; } = false;
    public static bool ShouldAcceptEverything { get; internal set; } = false;

    public static int ThreadLimit { get; internal set; } = Environment.ProcessorCount;
    public static int FileErrors { get; internal set; } = 0;
}
