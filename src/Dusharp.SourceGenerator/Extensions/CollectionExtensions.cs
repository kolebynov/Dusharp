using System.Collections.Generic;

namespace Dusharp.Extensions;

public static class CollectionExtensions
{
	public static int IndexOf<T>(this IEnumerable<T> collection, T value)
	{
		var comparer = EqualityComparer<T>.Default;

		var index = 0;
		foreach (var item in collection)
		{
			if (comparer.Equals(item, value))
			{
				return index;
			}

			index++;
		}

		return -1;
	}
}