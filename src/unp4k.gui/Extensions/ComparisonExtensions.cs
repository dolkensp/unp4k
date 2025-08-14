using System;
using System.Globalization;

namespace unp4k.gui.Extensions
{
	public static class ComparisonExtensions
	{
		private static CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");

		public static Boolean Contains(this String haystack, String needle, CompareOptions compareOptions = CompareOptions.None)
		{
			return _culture.CompareInfo.IndexOf(haystack, needle, compareOptions) > -1;
		}
	}
}
