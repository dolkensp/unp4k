using System;

/// <summary>
/// The MIT License (MIT)
/// 
/// Copyright (c) 2021 Connor 'Stryxus' Shearer
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </summary>
public static class Logger
{
    private static int PreviousMessageCount;

    static Logger()
    {
        try
        {
            Console.BufferWidth = Console.WindowWidth;
            ClearBuffer();
        }
        catch
        {
            Console.WriteLine("[CRITICAL]: Logger is unable to initialise!");
        }
    }

    private static void PushLog(LogPackage pckg)
    {
        if (pckg.Level == 1) Console.ForegroundColor = ConsoleColor.Yellow;
        else if (pckg.Level == 2) Console.ForegroundColor = ConsoleColor.Red;
        else Console.ForegroundColor = ConsoleColor.White;
        if (pckg.ClearMode == 0)
        {
            if (pckg.Level == -1) Console.WriteLine(pckg.Message);
            else if (pckg.Level == 0 && PreviousMessageCount == 0) Console.WriteLine("[" + pckg.PostTime.ToString("yyyy/MM/dd HH:mm:ss.fff") + "][INFO]:  " + pckg.Message);
            else if (pckg.Level == 0) ClearLine("[" + pckg.PostTime.ToString("yyyy/MM/dd HH:mm:ss.fff") + "][INFO]:  [" + PreviousMessageCount + " Duplicates] " + pckg.Message);
            else if (pckg.Level == 1 && PreviousMessageCount == 0) Console.WriteLine("[" + pckg.PostTime.ToString("yyyy/MM/dd HH:mm:ss.fff") + "][WARN]:  " + pckg.Message);
            else if (pckg.Level == 1) ClearLine("[" + pckg.PostTime.ToString("yyyy/MM/dd HH:mm:ss.fff") + "][WARN]:  [" + PreviousMessageCount + " Duplicates] " + pckg.Message);
            else if (pckg.Level == 2 && PreviousMessageCount == 0) Console.WriteLine("[" + pckg.PostTime.ToString("yyyy/MM/dd HH:mm:ss.fff") + "][ERROR]: " + pckg.Message);
            else if (pckg.Level == 2) ClearLine("[" + pckg.PostTime.ToString("yyyy/MM/dd HH:mm:ss.fff") + "][ERROR]:  [" + PreviousMessageCount + " Duplicates] " + pckg.Message);
            else if (pckg.Level == 3 && PreviousMessageCount == 0) Console.WriteLine("[" + pckg.PostTime.ToString("yyyy/MM/dd HH:mm:ss.fff") + "][DEBUG]: " + pckg.Message);
            else if (pckg.Level == 3) ClearLine("[" + pckg.PostTime.ToString("yyyy/MM/dd HH:mm:ss.fff") + "][DEBUG]:  [" + PreviousMessageCount + " Duplicates] " + pckg.Message);
            else Console.WriteLine("[" + pckg.PostTime.ToString("yyyy/MM/dd HH:mm:ss.fff") + "][INFO]: " + pckg.Message);
        }
        else if (pckg.ClearMode == 3) Console.Clear();
        else if (pckg.ClearMode == 2) Console.WriteLine();
        else if (pckg.ClearMode == 1) Console.WriteLine(pckg.Message);
    }

    public static void Log(object msg)
    {
        LogPackage pckg = default;
        pckg.Level = -1;
        pckg.Message = msg is not null ? msg.ToString() : "null";
        PushLog(pckg);
    }

    public static void LogInfo(object msg)
    {
        LogPackage pckg = default;
        pckg.PostTime = DateTime.Now;
        pckg.Level = 0;
        pckg.Message = msg is not null ? msg.ToString() : "null";
        PushLog(pckg);
    }

    public static void LogWarn(object msg)
    {
        LogPackage pckg = default;
        pckg.PostTime = DateTime.Now;
        pckg.Level = 1;
        pckg.Message = msg is not null ? msg.ToString() : "null";
        PushLog(pckg);
    }

    public static void LogError(object msg)
    {
        LogPackage pckg = default;
        pckg.PostTime = DateTime.Now;
        pckg.Level = 2;
        pckg.Message = msg is not null ? msg.ToString() : "null";
        PushLog(pckg);
    }

#if DEBUG
    public static void LogDebug(object msg)
    {
        LogPackage pckg = default;
        pckg.PostTime = DateTime.Now;
        pckg.Level = 3;
        pckg.Message = msg is not null ? msg.ToString() : "null";
        PushLog(pckg);
    }
#else
    public static void LogDebug(object msg)
    {
        LogPackage pckg = default;
        pckg.PostTime = DateTime.Now;
        pckg.Level = 3;
        pckg.Message = "Debug logs should not be called in Release mode!";
        PushLog(pckg);
    }
#endif

    public static void LogException<T>(T e) where T : Exception
    {
        LogPackage pckg = default;
        pckg.PostTime = DateTime.Now;
        pckg.Level = 2;
        pckg.Message = $"Source: {e.Source}\n | Data: {e.Data}\n | Message: {e.Message}\n | StackTrace: {e.StackTrace}";
        PushLog(pckg);
    }

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
        string text = string.Empty;
        for (int i = 0; i < Console.BufferWidth - 1; i++) text += "-";
        LogPackage pckg = default;
        pckg.ClearMode = 1;
        pckg.Message = text;
        PushLog(pckg);
    }

    public static void ClearLine(string? content = null)
    {
        if (string.IsNullOrEmpty(content))
        {
            content = string.Empty;
            for (int i = 0; i < Console.BufferWidth - 1; i++) content += " ";
        }
        Console.Write("\r{0}", content);
    }

    public static void ClearBuffer()
    {
        LogPackage pckg = default;
        pckg.ClearMode = 3;
        PushLog(pckg);
    }

    internal struct LogPackage
    {
        internal DateTime PostTime { get; set; }
        internal int ClearMode { get; set; }
        internal int Level { get; set; }
        internal string Message { get; set; }
    }
}