#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for array
/// </summary>
public static class ArrayExtensions
{
	#region Methods

	/// <summary>
	/// Check index and length to ensure it is within bounds of the array.
	/// </summary>
	/// <param name="array"> The array to check. </param>
	/// <param name="index"> The index to start in the array. </param>
	/// <param name="length"> The length to the end index of the array. </param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void BoundsCheckArray<T>(this T[] array, int index, int length)
	{
		if (array == null)
		{
			throw new ArgumentNullException(nameof(array));
		}

		if ((index < 0) || (index >= array.Length))
		{
			throw new IndexOutOfRangeException(Babel.Tower[BabelKeys.IndexOutOfRange]);
		}

		if ((length < 0) || ((index + length) > array.Length))
		{
			throw new IndexOutOfRangeException(Babel.Tower[BabelKeys.IndexAndLengthOutOfRange]);
		}
	}

	/// <summary>
	/// Combine many arrays into a single arrays.
	/// </summary>
	/// <param name="first"> The first array to start. </param>
	/// <param name="arrays"> The arrays to combine. </param>
	/// <typeparam name="T"> The types contained in the arrays. </typeparam>
	/// <returns> The combine array. </returns>
	public static T[] Combine<T>(this T[] first, params T[][] arrays)
	{
		var length = first.Length;
		foreach (var array in arrays)
		{
			length += array.Length;
		}
		var result = new T[length];
		length = first.Length;
		Array.Copy(first, 0, result, 0, first.Length);
		foreach (var array in arrays)
		{
			Array.Copy(array, 0, result, length, array.Length);
			length += array.Length;
		}
		return result;
	}

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
	/// Read a source array in a chunked size.
	/// </summary>
	/// <typeparam name="T"> The type that is in the array. </typeparam>
	/// <param name="source"> The source array. </param>
	/// <param name="chunk"> The chunk length. </param>
	/// <returns> The enumerator for chunks. </returns>
	public static IEnumerable<T[]> EnumerateChunks<T>(this T[] source, int chunk)
	{
		var response = new T[chunk];
		if ((source.Length % chunk) != 0)
		{
			throw new ArgumentException("The chunk size is invalid for source array length.", nameof(chunk));
		}

		for (var i = 0; i < source.Length; i += chunk)
		{
			Array.Copy(source, i, response, 0, chunk);
			yield return response;
		}
	}

	/// <summary>
	/// Get the value if the index exist otherwise return default value.
	/// </summary>
	/// <typeparam name="T"> The type of the items in teh array. </typeparam>
	/// <param name="array"> The array. </param>
	/// <param name="index"> The index in the array to read. </param>
	/// <param name="defaultValue"> The value to provide if the index is invalid. </param>
	/// <returns> The value or the default value. </returns>
	public static T GetArrayValueOrDefault<T>(this T[] array, int index, T defaultValue)
	{
		if ((index >= 0) && (index < array.Length))
		{
			return array[index];
		}

		return defaultValue;
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

		return enumerable.Cast<object>().ToList();
	}

	/// <summary>
	/// Gets a sub array from an existing array.
	/// </summary>
	/// <typeparam name="T"> The type of the items in teh array. </typeparam>
	/// <param name="data"> The array to pull from. </param>
	/// <param name="index"> The index to start from. </param>
	/// <param name="length"> The amount of data to pull. </param>
	/// <returns> The sub array of data. </returns>
	public static T[] SubArray<T>(this T[] data, int index, int length)
	{
		var result = new T[length];
		Array.Copy(data, index, result, 0, length);
		return result;
	}

	#endregion
}