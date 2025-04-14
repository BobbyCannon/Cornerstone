#region References

using System;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Document;

/// <summary>
/// Extension methods for <see cref="IRange" />.
/// </summary>
public static class SegmentExtensions
{
	#region Fields

	public static readonly SimpleRange Invalid = new(-1, -1);

	#endregion

	#region Methods

	/// <summary>
	/// Gets whether <paramref name="range" /> fully contains the specified segment.
	/// </summary>
	/// <remarks>
	/// Use <c> segment.Contains(offset, 0) </c> to detect whether a segment (end inclusive) contains offset;
	/// use <c> segment.Contains(offset, 1) </c> to detect whether a segment (end exclusive) contains offset.
	/// </remarks>
	public static bool Contains(this IRange range, int offset, int length)
	{
		return Contains(range, new SimpleRange(offset, length));
	}

	/// <summary>
	/// Gets whether <paramref name="value" /> fully contains the specified segment.
	/// </summary>
	public static bool Contains(this IRange value, IRange newValue)
	{
		if ((value == null) || (newValue == null))
		{
			return false;
		}

		return value.GetOverlap(newValue) != Invalid;
	}

	/// <summary>
	/// Gets the overlapping portion of the segments.
	/// Returns SegmentExtensions.Invalid if the segments don't overlap.
	/// </summary>
	public static SimpleRange GetOverlap(this IRange segment1, IRange segment2)
	{
		var start = Math.Max(segment1.StartIndex, segment2.StartIndex);
		var end = Math.Min(segment1.EndIndex, segment2.EndIndex);
		return end < start ? Invalid : new SimpleRange(start, end - start);
	}

	#endregion
}