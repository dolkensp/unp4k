using System.Diagnostics;

namespace unlib;

public static class Platform
{
    public static void OpenFileManager(string path)
    {
        switch (OS.Type)
        {
            case OSType.Windows:
                Process.Start("explorer", path);
                break;
            case OSType.Linux:
                Process.Start("nautilus", path);
                break;
            case OSType.MacOSX:
                Process.Start(path);
                break;
            case OSType.Android:
                Process.Start(path);
                break;
            case OSType.iPhone:
                Process.Start(path);
                break;
        }
    }

    public static void OpenWebpage(Uri url)
    {
        switch (OS.Type)
        {
            case OSType.Windows:
                Process.Start(new ProcessStartInfo { FileName = url.AbsoluteUri, UseShellExecute = true });
                break;
            case OSType.Linux:
                Process.Start(new ProcessStartInfo { FileName = url.AbsoluteUri, UseShellExecute = true });
                break;
            case OSType.MacOSX:
                Process.Start(new ProcessStartInfo { FileName = url.AbsoluteUri, UseShellExecute = true });
                break;
            case OSType.Android:
                Process.Start(new ProcessStartInfo { FileName = url.AbsoluteUri, UseShellExecute = true });
                break;
            case OSType.iPhone:
                Process.Start(new ProcessStartInfo { FileName = url.AbsoluteUri, UseShellExecute = true });
                break;
        }
    }
}