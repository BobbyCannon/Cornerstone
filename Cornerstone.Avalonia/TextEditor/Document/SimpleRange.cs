#region References

using System;
using System.Diagnostics;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Document;

/// <summary>
/// Represents a simple segment (Offset,Length pair) that is not automatically updated
/// on document changes.
/// </summary>
public readonly struct SimpleRange : IEquatable<SimpleRange>, IRange
{
	#region Fields

	public readonly int Offset, Length;

	#endregion

	#region Constructors

	public SimpleRange(int offset, int length)
	{
		Offset = offset;
		Length = length;
	}

	public SimpleRange(IRange range)
	{
		Debug.Assert(range != null);
		Offset = range.StartIndex;
		Length = range.Length;
	}

	#endregion

	#region Properties

	public int EndIndex => Offset + Length;

	int IRange.Length => Length;

	int IRange.StartIndex => Offset;

	#endregion

	#region Methods

	public override bool Equals(object obj)
	{
		return obj is SimpleRange && Equals((SimpleRange) obj);
	}

	public bool Equals(SimpleRange other)
	{
		return (Offset == other.Offset) && (Length == other.Length);
	}

	public static SimpleRange FromIndexes(int startIndex, int endIndex)
	{
		return new SimpleRange(startIndex, endIndex - startIndex);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return Offset + (10301 * Length);
		}
	}

	public static bool operator ==(SimpleRange left, SimpleRange right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(SimpleRange left, SimpleRange right)
	{
		return !left.Equals(right);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"[Offset={Offset}, Length={Length}]";
	}

	#endregion
}