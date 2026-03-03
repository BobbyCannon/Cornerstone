#region References

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Utils;

internal class StableSort
{
	#region Methods

	public static List<int> SortedMap<T>(IReadOnlyList<T> elements, Comparison<int> compare)
	{
		var map = new List<int>(elements.Count);
		for (var i = 0; i < elements.Count; i++)
		{
			map.Add(i);
		}

		var span = CollectionsMarshal.AsSpan(map);
		SortHelper<int>.Sort(span, compare);
		return map;
	}

	#endregion
}