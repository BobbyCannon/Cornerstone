#region References

using System;
using System.Collections;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Extensions;

internal static class IEnumerableExtensions
{
	#region Methods

	public static object ElementAt(this IEnumerable source, int index)
	{
		var i = -1;
		var enumerator = source.GetEnumerator();

		while (enumerator.MoveNext() && (++i < index))
		{
			;
		}
		if (i == index)
		{
			return enumerator.Current;
		}
		throw new ArgumentOutOfRangeException(nameof(index));
	}

	#endregion
}