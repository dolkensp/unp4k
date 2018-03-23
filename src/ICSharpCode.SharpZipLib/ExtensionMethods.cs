using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpZipLib
{
	public static class ExtensionMethods
	{
		/// <summary>
		/// From http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa/14333437#14333437
		/// </summary>
		/// <param name="bytes">Array of bytes to convert to hex string</param>
		/// <returns>A hex string representation of the input bytes</returns>
		public static String ToHex(this Byte[] bytes)
		{
			Char[] buffer = new Char[bytes.Length * 2];
			Int32 b;
			for (Int32 i = 0; i < bytes.Length; i++)
			{
				b = bytes[i] >> 4;
				buffer[i * 2] = (Char)(87 + b + (((b - 10) >> 31) & -39));
				b = bytes[i] & 0xF;
				buffer[i * 2 + 1] = (Char)(87 + b + (((b - 10) >> 31) & -39));
			}

			return new String(buffer);
		}
	}
}
