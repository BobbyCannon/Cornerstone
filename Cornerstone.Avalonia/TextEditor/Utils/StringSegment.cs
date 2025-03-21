#region References

using System;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Utils;

/// <summary>
/// Represents a string with a segment.
/// Similar to System.ArraySegment&lt;T&gt;, but for strings instead of arrays.
/// </summary>
public struct StringSegment : IEquatable<StringSegment>
{
	#region Constructors

	/// <summary>
	/// Creates a new StringSegment.
	/// </summary>
	public StringSegment(string text, int offset, int count)
	{
		if (text == null)
		{
			throw new ArgumentNullException(nameof(text));
		}
		if ((offset < 0) || (offset > text.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(offset));
		}
		if ((offset + count) > text.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}
		Text = text;
		Offset = offset;
		Count = count;
	}

	/// <summary>
	/// Creates a new StringSegment.
	/// </summary>
	public StringSegment(string text)
	{
		Text = text ?? throw new ArgumentNullException(nameof(text));
		Offset = 0;
		Count = text.Length;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the length of the segment.
	/// </summary>
	public int Count { get; }

	/// <summary>
	/// Gets the start offset of the segment with the text.
	/// </summary>
	public int Offset { get; }

	/// <summary>
	/// Gets the string used for this segment.
	/// </summary>
	public string Text { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		if (obj is StringSegment)
		{
			return Equals((StringSegment) obj); // use Equals method below
		}
		return false;
	}

	/// <inheritdoc />
	public bool Equals(StringSegment other)
	{
		// add comparisions for all members here
		return ReferenceEquals(Text, other.Text) && (Offset == other.Offset) && (Count == other.Count);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return Text.GetHashCode() ^ Offset ^ Count;
	}

	/// <summary>
	/// Equality operator.
	/// </summary>
	public static bool operator ==(StringSegment left, StringSegment right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Inequality operator.
	/// </summary>
	public static bool operator !=(StringSegment left, StringSegment right)
	{
		return !left.Equals(right);
	}

	#endregion
}