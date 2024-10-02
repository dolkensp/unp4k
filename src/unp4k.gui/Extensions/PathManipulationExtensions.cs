using System;

namespace unp4k.gui.Extensions
{
	public static class PathManipulationExtensions
	{
		public static String RelativeTo(this String fullPath, String rootPath = null)
		{
			rootPath = rootPath ?? String.Empty;

			if (fullPath.StartsWith(rootPath, StringComparison.InvariantCultureIgnoreCase))
			{
				return fullPath.Substring(rootPath.Length).TrimStart('\\');
			}

			return fullPath;
		}
	}
}
