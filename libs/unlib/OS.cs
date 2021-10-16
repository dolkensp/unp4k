using System;

namespace unlib
{
    public static class OS
    {
        public static OSType Type { get; private set; }

        public static bool IsWindows => Type == OSType.Windows;

        public static bool IsLinux => Type == OSType.Linux;

        public static bool IsAndroid => Type == OSType.Android;

        public static bool IsMacOSX => Type == OSType.MacOSX;

        public static bool IsiPhone => Type == OSType.iPhone;

        static OS()
        {
            Type = (Environment.OSVersion.Platform != PlatformID.Win32NT) ? ((Environment.OSVersion.Platform == PlatformID.Unix) ? OSType.Linux : ((Environment.OSVersion.Platform == PlatformID.MacOSX) ? OSType.MacOSX : OSType.Windows)) : OSType.Windows;
        }
    }

    public enum OSType
    {
        Windows,
        Linux,
        Android,
        MacOSX,
        iPhone
    }
}
