using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
