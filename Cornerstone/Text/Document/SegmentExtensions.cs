#region References

using System;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// Extension methods for <see cref="ISegment" />.
/// </summary>
public static class SegmentExtensions
{
	#region Fields

	public static readonly SimpleSegment Invalid = new(-1, -1);

	#endregion

	#region Methods

	/// <summary>
	/// Gets whether <paramref name="segment" /> fully contains the specified segment.
	/// </summary>
	/// <remarks>
	/// Use <c> segment.Contains(offset, 0) </c> to detect whether a segment (end inclusive) contains offset;
	/// use <c> segment.Contains(offset, 1) </c> to detect whether a segment (end exclusive) contains offset.
	/// </remarks>
	public static bool Contains(this ISegment segment, int offset, int length)
	{
		return Contains(segment, new SimpleSegment(offset, length));
	}

	/// <summary>
	/// Gets whether <paramref name="value" /> fully contains the specified segment.
	/// </summary>
	public static bool Contains(this ISegment value, ISegment newValue)
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
	public static SimpleSegment GetOverlap(this ISegment segment1, ISegment segment2)
	{
		var start = Math.Max(segment1.Offset, segment2.Offset);
		var end = Math.Min(segment1.EndOffset, segment2.EndOffset);
		return end < start ? Invalid : new SimpleSegment(start, end - start);
	}

	#endregion
}