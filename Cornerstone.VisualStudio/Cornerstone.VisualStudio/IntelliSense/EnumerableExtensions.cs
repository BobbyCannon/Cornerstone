#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

internal static class EnumerableExtensions
{
	#region Methods

	public static TSource FirstOrDefault<TSource, TArg>(this IEnumerable<TSource> source, Func<TSource, TArg, bool> predicate, TArg arg)
	{
		if (source is null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if (predicate is null)
		{
			throw new ArgumentNullException(nameof(predicate));
		}

		var enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			var item = enumerator.Current;
			if (predicate(item, arg))
			{
				return item;
			}
		}
		return default;
	}

	#endregion
}