using System;

namespace unp4k.gui.Extensions
{
	public static class ParserExtensions
	{
		public static Boolean ToBoolean(this String input, Boolean @default)
		{
			Boolean.TryParse(input, out @default);
			return @default;
		}
	}
}
