#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for array
/// </summary>
public static class ArrayExtensions
{
	#region Methods

	/// <summary>
	/// Combine many arrays into a single arrays.
	/// </summary>
	/// <param name="arrays"> The arrays to combine. </param>
	/// <typeparam name="T"> The types contained in the arrays. </typeparam>
	/// <returns> The combine array. </returns>
	public static T[] CombineArrays<T>(params T[][] arrays)
	{
		var length = arrays.Sum(array => array.Length);
		var result = new T[length];
		var offset = 0;

		foreach (var array in arrays)
		{
			Array.Copy(array, 0, result, offset, array.Length);
			offset += array.Length;
		}

		return result;
	}

	/// <summary>
	/// Returns the enumerable as an IList with minimal copying when possible.
	/// Supports List, Array, HashSet, LinkedList, Queue, Stack, and other common collections.
	/// </summary>
	public static IList IterateList(this IEnumerable enumerable)
	{
		if (enumerable is null)
		{
			return Array.Empty<object>();
		}

		if (enumerable is IList list)
		{
			return list;
		}

		// Fallback for non-generic ICollection (older collections, some concurrent types)
		if (enumerable is ICollection collection)
		{
			var arr = new object[collection.Count];
			collection.CopyTo(arr, 0);
			return arr;
		}

		// Slow path for pure IEnumerable (custom iterators, complex LINQ queries without Count)
		var result = new List<object>();
		foreach (var item in enumerable)
		{
			result.Add(item);
		}

		return result;
	}

	#endregion
}