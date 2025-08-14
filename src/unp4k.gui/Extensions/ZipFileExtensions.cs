using ICSharpCode.SharpZipLib.Zip;
using System;

namespace unp4k.gui.Extensions
{
	public static class ZipFileExtensions
    {
		public static String GetArchiveName(this ZipFile zipFile, String name)
		{
			if (String.IsNullOrWhiteSpace(name)) name = zipFile.Name;
			if (String.IsNullOrWhiteSpace(name)) name = "Data.p4k";

			return name;
		}
    }
}
