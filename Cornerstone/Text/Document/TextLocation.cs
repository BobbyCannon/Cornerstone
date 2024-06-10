#region References

using System;
using System.ComponentModel;
using System.Globalization;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// A line/column position.
/// Text editor lines/columns are counted started from one.
/// </summary>
/// <remarks>
/// The document provides the methods <see cref="IDocument.GetLocation" /> and
/// <see cref="IDocument.GetOffset(TextLocation)" /> to convert between offsets and TextLocations.
/// </remarks>
[TypeConverter(typeof(TextLocationConverter))]
public readonly struct TextLocation : IComparable<TextLocation>, IEquatable<TextLocation>
{
	#region Fields

	/// <summary>
	/// Represents no text location (0, 0).
	/// </summary>
	public static readonly TextLocation Empty = new(0, 0);

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a TextLocation instance.
	/// </summary>
	public TextLocation(int line, int column)
	{
		Line = line;
		Column = column;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the column number.
	/// </summary>
	public int Column { get; }

	/// <summary>
	/// Gets whether the TextLocation instance is empty.
	/// </summary>
	public bool IsEmpty => (Column <= 0) && (Line <= 0);

	/// <summary>
	/// Gets the line number.
	/// </summary>
	public int Line { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Compares two text locations.
	/// </summary>
	public int CompareTo(TextLocation other)
	{
		if (this == other)
		{
			return 0;
		}
		if (this < other)
		{
			return -1;
		}
		return 1;
	}

	/// <summary>
	/// Equality test.
	/// </summary>
	public override bool Equals(object obj)
	{
		if (!(obj is TextLocation))
		{
			return false;
		}
		return (TextLocation) obj == this;
	}

	/// <summary>
	/// Equality test.
	/// </summary>
	public bool Equals(TextLocation other)
	{
		return this == other;
	}

	/// <summary>
	/// Gets a hash code.
	/// </summary>
	public override int GetHashCode()
	{
		return unchecked((191 * Column.GetHashCode()) ^ Line.GetHashCode());
	}

	/// <summary>
	/// Equality test.
	/// </summary>
	public static bool operator ==(TextLocation left, TextLocation right)
	{
		return (left.Column == right.Column) && (left.Line == right.Line);
	}

	/// <summary>
	/// Compares two text locations.
	/// </summary>
	public static bool operator >(TextLocation left, TextLocation right)
	{
		if (left.Line > right.Line)
		{
			return true;
		}
		if (left.Line == right.Line)
		{
			return left.Column > right.Column;
		}
		return false;
	}

	/// <summary>
	/// Compares two text locations.
	/// </summary>
	public static bool operator >=(TextLocation left, TextLocation right)
	{
		return !(left < right);
	}

	/// <summary>
	/// Inequality test.
	/// </summary>
	public static bool operator !=(TextLocation left, TextLocation right)
	{
		return (left.Column != right.Column) || (left.Line != right.Line);
	}

	/// <summary>
	/// Compares two text locations.
	/// </summary>
	public static bool operator <(TextLocation left, TextLocation right)
	{
		if (left.Line < right.Line)
		{
			return true;
		}
		if (left.Line == right.Line)
		{
			return left.Column < right.Column;
		}
		return false;
	}

	/// <summary>
	/// Compares two text locations.
	/// </summary>
	public static bool operator <=(TextLocation left, TextLocation right)
	{
		return !(left > right);
	}

	/// <summary>
	/// Gets a string representation for debugging purposes.
	/// </summary>
	public override string ToString()
	{
		return string.Format(CultureInfo.InvariantCulture, "(Line {1}, Col {0})", Column, Line);
	}

	#endregion
}

/// <summary>
/// Converts strings of the form '0+[;,]0+' to a <see cref="TextLocation" />.
/// </summary>
public class TextLocationConverter : TypeConverter
{
	#region Methods

	/// <inheritdoc />
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	/// <inheritdoc />
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		return destinationType == typeof(TextLocation);
	}

	/// <inheritdoc />
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		var s = value as string;
		var parts = s?.Split(';', ',');
		if (parts?.Length == 2)
		{
			return new TextLocation(int.Parse(parts[0], culture), int.Parse(parts[1], culture));
		}
		throw new InvalidOperationException();
	}

	/// <inheritdoc />
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (value is TextLocation loc && (destinationType == typeof(string)))
		{
			return loc.Line.ToString(culture) + ";" + loc.Column.ToString(culture);
		}
		throw new InvalidOperationException();
	}

	#endregion
}

/// <summary>
/// An (Offset,Length)-pair.
/// </summary>
public interface ISegment
{
	#region Properties

	/// <summary>
	/// Gets the end offset of the segment.
	/// </summary>
	/// <remarks> EndOffset = Offset + Length; </remarks>
	int EndOffset { get; }

	/// <summary>
	/// Gets the length of the segment.
	/// </summary>
	/// <remarks> For line segments (IDocumentLine), the length does not include the line delimiter. </remarks>
	int Length { get; }

	/// <summary>
	/// Gets the start offset of the segment.
	/// </summary>
	int Offset { get; }

	#endregion
}