#region References

using System;
using System.Diagnostics;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Document;

/// <summary>
/// Represents a bit range within a binary document.
/// </summary>
[DebuggerDisplay("[{Start}, {End})")]
public readonly struct BitRange : IEquatable<BitRange>
{
	#region Fields

	/// <summary>
	/// Represents the empty range.
	/// </summary>
	public static readonly BitRange Empty = new();

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new bit range.
	/// </summary>
	/// <param name="start"> The start byte offset. </param>
	/// <param name="end"> The (exclusive) end byte offset. </param>
	public BitRange(ulong start, ulong end)
		: this(new BitLocation(start), new BitLocation(end))
	{
	}

	/// <summary>
	/// Creates a new bit range.
	/// </summary>
	/// <param name="start"> The start location. </param>
	/// <param name="end"> The (exclusive) end location. </param>
	public BitRange(BitLocation start, BitLocation end)
	{
		if (end < start)
		{
			throw new ArgumentException("End location is smaller than start location.");
		}

		Start = start;
		End = end;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the total number of bits that the range spans.
	/// </summary>
	public ulong BitLength
	{
		get
		{
			var result = ByteLength * 8;
			result -= (ulong) Start.BitIndex;
			result += (ulong) End.BitIndex;
			return result;
		}
	}

	/// <summary>
	/// Gets the total number of bytes that the range spans.
	/// </summary>
	public ulong ByteLength => End.ByteIndex - Start.ByteIndex;

	/// <summary>
	/// Gets the exclusive end location of the range.
	/// </summary>
	public BitLocation End { get; }

	/// <summary>
	/// Gets a value indicating whether the range is empty or not.
	/// </summary>
	public bool IsEmpty => BitLength == 0;

	/// <summary>
	/// Gets the start location of the range.
	/// </summary>
	public BitLocation Start { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Restricts the range to the provided range.
	/// </summary>
	/// <param name="range"> The range to restrict to. </param>
	/// <returns> The restricted range. </returns>
	public BitRange Clamp(BitRange range)
	{
		var start = Start.Max(range.Start);
		var end = End.Min(range.End);
		if (start > end)
		{
			return Empty;
		}

		return new BitRange(start, end);
	}

	/// <summary>
	/// Determines whether the provided location is within the range.
	/// </summary>
	/// <param name="location"> The location. </param>
	/// <returns> <c> true </c> if the location is within the range, <c> false </c> otherwise. </returns>
	public bool Contains(BitLocation location)
	{
		return (location >= Start) && (location < End);
	}

	/// <summary>
	/// Determines whether the provided range falls completely within the current range.
	/// </summary>
	/// <param name="other"> The other range. </param>
	/// <returns> <c> true </c> if the provided range is completely enclosed, <c> false </c> otherwise. </returns>
	public bool Contains(BitRange other)
	{
		return Contains(other.Start) && Contains(other.End.PreviousOrZero());
	}

	/// <inheritdoc />
	public bool Equals(BitRange other)
	{
		return Start.Equals(other.Start) && End.Equals(other.End);
	}

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		return obj is BitRange other && Equals(other);
	}

	/// <summary>
	/// Extends the range to the provided location.
	/// </summary>
	/// <param name="location"> The location to extend to. </param>
	/// <returns> The extended range. </returns>
	public BitRange ExtendTo(BitLocation location)
	{
		return new(Start.Min(location), End.Max(location));
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		unchecked
		{
			return (Start.GetHashCode() * 397) ^ End.GetHashCode();
		}
	}

	/// <summary>
	/// Determines whether two ranges are equal.
	/// </summary>
	/// <param name="a"> The first range. </param>
	/// <param name="b"> The second range. </param>
	/// <returns> <c> true </c> if the ranges are equal, <c> false </c> otherwise. </returns>
	public static bool operator ==(BitRange a, BitRange b)
	{
		return a.Equals(b);
	}

	/// <summary>
	/// Determines whether two ranges are not equal.
	/// </summary>
	/// <param name="a"> The first range. </param>
	/// <param name="b"> The second range. </param>
	/// <returns> <c> true </c> if the ranges are not equal, <c> false </c> otherwise. </returns>
	public static bool operator !=(BitRange a, BitRange b)
	{
		return !a.Equals(b);
	}

	/// <summary>
	/// Determines whether the current range overlaps with the provided range.
	/// </summary>
	/// <param name="other"> The other range. </param>
	/// <returns> <c> true </c> if the range overlaps, <c> false </c> otherwise. </returns>
	public bool OverlapsWith(BitRange other)
	{
		return Contains(other.Start)
			|| Contains(other.End.PreviousOrZero())
			|| other.Contains(Start)
			|| other.Contains(End.PreviousOrZero());
	}

	/// <summary>
	/// Splits the range at the provided location.
	/// </summary>
	/// <param name="location"> The location to split at. </param>
	/// <returns> The two resulting ranges. </returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Occurs when the provided location does not fall within the current range.
	/// </exception>
	public (BitRange, BitRange) Split(BitLocation location)
	{
		if (!Contains(location))
		{
			throw new ArgumentOutOfRangeException(nameof(location));
		}

		return (
			new BitRange(Start, location),
			new BitRange(location, End)
		);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"[{Start}, {End})";
	}

	#endregion
}