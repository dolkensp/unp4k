using System.Text;

using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

using unlib;

public static class Logger
{
    private static Serilog.Core.Logger InternalConsoleLogger;

    static Logger()
    {
        InternalConsoleLogger = new LoggerConfiguration().WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message}{NewLine}{Exception}").CreateLogger();
        if (OS.IsWindows) Console.BufferWidth = Console.WindowWidth;
        ClearBuffer();
    }

    private static void PushLog(LogPackage pckg)
    {
        if (pckg.ClearMode is 0)
        {
            if (pckg.Level is -1) Console.WriteLine(pckg.Message);
            else if (pckg.Level is 0) InternalConsoleLogger.Information(pckg.Message);
            else if (pckg.Level is 1) InternalConsoleLogger.Warning(pckg.Message);
            else if (pckg.Level is 2) InternalConsoleLogger.Error(pckg.Message);
            else if (pckg.Level is 3) InternalConsoleLogger.Fatal(pckg.Message);
            else if (pckg.Level is 4) InternalConsoleLogger.Debug(pckg.Message);
            else InternalConsoleLogger.Information(pckg.Message);
        }
        else if (pckg.ClearMode is 3) Console.Clear();
        else if (pckg.ClearMode is 2) Console.WriteLine();
        else if (pckg.ClearMode is 1) Console.WriteLine(pckg.Message);
    }

    public static void LogInfo(object msg)
    {
        LogPackage pckg = default;
        pckg.Level = 0;
        pckg.Message = msg is not null ? msg.ToString() : "null";
        PushLog(pckg);
    }

    public static void LogWarn(object msg)
    {
        LogPackage pckg = default;
        pckg.Level = 1;
        pckg.Message = msg is not null ? msg.ToString() : "null";
        PushLog(pckg);
    }

    public static void LogError(object msg)
    {
        LogPackage pckg = default;
        pckg.Level = 2;
        pckg.Message = msg is not null ? msg.ToString() : "null";
        PushLog(pckg);
    }

    public static void LogException<T>(T e) where T : Exception
    {
        LogPackage pckg = default;
        pckg.Level = 3;
        pckg.Message = $"Source: {e.Source}\n | Data: {e.Data}\n | Message: {e.Message}\n | StackTrace: {e.StackTrace}";
        PushLog(pckg);
    }

#if DEBUG
    public static void LogDebug(object msg)
    {
        LogPackage pckg = default;
        pckg.Level = 3;
        pckg.Message = msg is not null ? msg.ToString() : "null";
        PushLog(pckg);
    }
#else
    public static void LogDebug(object msg)
    {
        LogPackage pckg = default;
        pckg.Level = 4;
        pckg.Message = "Debug logs should not be called in Release mode!";
        PushLog(pckg);
    }
#endif

    public static void NewLine(int lines = 1)
    {
        if (lines < 1) lines = 1;
        for (int i = 0; i < lines; i++)
        {
            LogPackage pckg = default;
            pckg.ClearMode = 2;
            PushLog(pckg);
        }
    }

    public static void DivideBuffer()
    {
        StringBuilder b = new();
        for (int i = 0; i < Console.BufferWidth - 1; i++) b.Append("-");
        LogPackage pckg = default;
        pckg.ClearMode = 1;
        pckg.Message = b.ToString();
        PushLog(pckg);
    }

    public static void ClearLine(string? content = null)
    {
        StringBuilder b = new(content is null ? string.Empty : content);
        for (int i = 0; i < Console.BufferWidth - 1; i++) b.Append(" ");
        Console.Write("\r{0}", b.ToString());
    }

    public static void ClearBuffer()
    {
        LogPackage pckg = default;
        pckg.ClearMode = 3;
        PushLog(pckg);
    }

    internal struct LogPackage
    {
        internal int ClearMode { get; set; }
        internal int Level { get; set; }
        internal string Message { get; set; }
    }
}