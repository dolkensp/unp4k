using System.Collections.Generic;

namespace unp4k.gui.Extensions
{
	public static class ListExtensions
	{
		public static IEnumerable<T> With<T>(this IEnumerable<T> list, T element)
		{
			foreach (var item in list)
			{
				yield return item;
			}

			yield return element;
		}
	}
}
