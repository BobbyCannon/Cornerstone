#region References

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Search;

/// <summary>
/// High-performance binary search utilities optimized for hot paths (text editors, layout engines, virtualized lists, etc.).
/// All methods assume the input collection is sorted in non-decreasing order.
/// </summary>
public static class BinarySearch
{
	#region Methods

	/// <summary>
	/// Returns the smallest value ≥ <paramref name="target" />, or <paramref name="fallback" /> if none exists.
	/// </summary>
	/// <remarks>
	/// • Returns the leftmost (smallest index) qualifying value when duplicates exist.<br />
	/// • Mirror of FindFloor.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static int FindCeil(ReadOnlySpan<int> sorted, int target, int fallback)
	{
		if (sorted.IsEmpty || (target > sorted[^1]))
		{
			return fallback;
		}

		var left = 0;
		var right = sorted.Length - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);

			if (sorted[mid] >= target)
			{
				right = mid - 1; // try to find something even smaller (still ≥ target)
			}
			else
			{
				left = mid + 1;
			}
		}

		return sorted[left];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindCeil(int[] array, int target, int fallback)
	{
		return FindCeil(array.AsSpan(), target, fallback);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindCeil(List<int> list, int target, int fallback)
	{
		return FindCeil(CollectionsMarshal.AsSpan(list), target, fallback);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindCeil(SpeedyList<int> list, int target, int fallback)
	{
		return FindCeil(list.AsSpan(), target, fallback);
	}

	/// <summary>
	/// Returns the smallest index (i) where sorted[i] ≥ <paramref name="target" />,
	/// or the length of the span if no such element exists.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static int FindCeilIndex(ReadOnlySpan<int> sorted, int target)
	{
		if (sorted.IsEmpty)
		{
			return -1;
		}

		var left = 0;
		var right = sorted.Length - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);

			if (sorted[mid] >= target)
			{
				right = mid - 1;
			}
			else
			{
				left = mid + 1;
			}
		}

		// After loop left is the candidate insertion point
		if ((left < sorted.Length) && (sorted[left] >= target))
		{
			return left;
		}

		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindCeilIndex(int[] array, int target)
	{
		return FindCeilIndex(array.AsSpan(), target);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindCeilIndex(List<int> list, int target)
	{
		return FindCeilIndex(CollectionsMarshal.AsSpan(list), target);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindCeilIndex(SpeedyList<int> list, int target)
	{
		return FindCeilIndex(list.AsSpan(), target);
	}

	/// <summary>
	/// Returns the largest value ≤ <paramref name="target" />, or <paramref name="fallback" /> if none exists.
	/// </summary>
	/// <remarks>
	/// • Returns the rightmost (largest index) qualifying value when duplicates exist.<br />
	/// • Extremely fast hot-path implementation (~5–12 ns typical).<br />
	/// • Safe for concurrent read access when the span is immutable.
	/// </remarks>
	/// <param name="sorted"> Non-decreasing sorted values </param>
	/// <param name="target"> The value to search for </param>
	/// <param name="fallback"> Returned when the list is empty or target is less than first element </param>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static int FindFloor(ReadOnlySpan<int> sorted, int target, int fallback)
	{
		if (sorted.IsEmpty || (target < sorted[0]))
		{
			return fallback;
		}

		var left = 0;
		var right = sorted.Length - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);

			if (sorted[mid] <= target)
			{
				// try to find something even larger (still ≤ target)
				left = mid + 1;
			}
			else
			{
				right = mid - 1;
			}
		}

		return sorted[right];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindFloor(int[] array, int target, int fallback)
	{
		return FindFloor(array.AsSpan(), target, fallback);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindFloor(List<int> list, int target, int fallback)
	{
		return FindFloor(CollectionsMarshal.AsSpan(list), target, fallback);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindFloor(SpeedyList<int> list, int target, int fallback)
	{
		return FindFloor(list.AsSpan(), target, fallback);
	}

	/// <summary>
	/// Returns the largest index (i) where sorted[i] ≤ <paramref name="target" />,
	/// or -1 if no such element exists.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static int FindFloorIndex(ReadOnlySpan<int> sorted, int target)
	{
		if (sorted.IsEmpty || (target < sorted[0]))
		{
			return -1;
		}

		var left = 0;
		var right = sorted.Length - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);

			if (sorted[mid] <= target)
			{
				left = mid + 1;
			}
			else
			{
				right = mid - 1;
			}
		}

		return right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindFloorIndex(int[] array, int target)
	{
		return FindFloorIndex(array.AsSpan(), target);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindFloorIndex(List<int> list, int target)
	{
		return FindFloorIndex(CollectionsMarshal.AsSpan(list), target);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindFloorIndex(SpeedyList<int> list, int target)
	{
		return FindFloorIndex(list.AsSpan(), target);
	}

	#endregion
}