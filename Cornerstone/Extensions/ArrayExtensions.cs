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
	/// Iterate the list.
	/// </summary>
	/// <param name="enumerable"> The items to enumerate. </param>
	/// <returns> The items in a list. </returns>
	public static IList IterateList(this IEnumerable enumerable)
	{
		if (enumerable is IList list)
		{
			return list;
		}

		if (enumerable is ICollection collection)
		{
			var arr = new object[collection.Count];
			collection.CopyTo(arr, 0);
			return arr;
		}

		var result = new List<object>();
		foreach (var item in enumerable)
		{
			result.Add(item);
		}

		return result;
	}

	#endregion
}