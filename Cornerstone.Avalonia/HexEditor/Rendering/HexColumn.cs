#region References

using System;
using Avalonia;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.HexEditor.Document;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

/// <summary>
/// Represents a column that renders binary data using hexadecimal number encoding.
/// </summary>
public class HexColumn : CellBasedColumn
{
	#region Fields

	/// <summary>
	/// Defines the <see cref="IsUppercase" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsUppercaseProperty =
		AvaloniaProperty.Register<HexColumn, bool>(nameof(IsUppercase), true);

	#endregion

	#region Constructors

	static HexColumn()
	{
		IsUppercaseProperty.Changed.AddClassHandler<HexColumn, bool>(OnIsUpperCaseChanged);
		CursorProperty.OverrideDefaultValue<HexColumn>(IBeamCursor);
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override int BitsPerCell => 4;

	/// <inheritdoc />
	public override int CellsPerWord => 2;

	/// <inheritdoc />
	public override double GroupPadding => CellSize.Width;

	/// <summary>
	/// Gets or sets a value indicating whether the hexadecimal digits should be rendered in uppercase or not.
	/// </summary>
	public bool IsUppercase
	{
		get => GetValue(IsUppercaseProperty);
		set => SetValue(IsUppercaseProperty, value);
	}

	/// <inheritdoc />
	public override Size MinimumSize => default;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override TextLine CreateTextLine(VisualBytesLine line)
	{
		if (HexView is null)
		{
			return null;
		}

		var properties = GetTextRunProperties();
		return TextFormatter.Current.FormatLine(
			new HexTextSource(this, line, properties),
			0,
			double.MaxValue,
			new GenericTextParagraphProperties(properties)
		);
	}

	/// <inheritdoc />
	public override string GetText(BitRange range)
	{
		if (HexView?.Document is null)
		{
			return null;
		}

		var data = new byte[range.ByteLength];
		HexView.Document.Read(range.Start.ByteIndex, data);

		var output = new char[(data.Length * 3) - 1];
		GetText(data, range, output);

		return new string(output);
	}

	/// <inheritdoc />
	protected override string PrepareTextInput(string input)
	{
		return input.Replace(" ", "");
	}

	/// <inheritdoc />
	protected override bool TryWriteCell(Span<byte> buffer, BitLocation bufferStart, BitLocation writeLocation, char input)
	{
		if (ParseNibble(input) is not { } nibble)
		{
			return false;
		}

		var relativeIndex = (int) (writeLocation.ByteIndex - bufferStart.ByteIndex);
		buffer[relativeIndex] = writeLocation.BitIndex == 4
			? (byte) ((buffer[relativeIndex] & 0xF) | (nibble << 4))
			: (byte) ((buffer[relativeIndex] & 0xF0) | nibble);
		return true;
	}

	private static char GetHexDigit(byte nibble, bool uppercase)
	{
		return nibble switch
		{
			< 10 => (char) (nibble + '0'),
			< 16 => (char) ((nibble - 10) + (uppercase ? 'A' : 'a')),
			_ => throw new ArgumentOutOfRangeException(nameof(nibble))
		};
	}

	private void GetText(ReadOnlySpan<byte> data, BitRange dataRange, Span<char> buffer)
	{
		var uppercase = IsUppercase;
		var invalidCellChar = InvalidCellChar;

		if (HexView?.Document?.ValidRanges is not { } valid)
		{
			buffer.Fill(invalidCellChar);
			return;
		}

		var index = 0;
		for (var i = 0; i < data.Length; i++)
		{
			if (i > 0)
			{
				buffer[index++] = ' ';
			}

			var location1 = new BitLocation(dataRange.Start.ByteIndex + (ulong) i, 0);
			var location2 = new BitLocation(dataRange.Start.ByteIndex + (ulong) i, 4);
			var location3 = new BitLocation(dataRange.Start.ByteIndex + (ulong) i + 1, 0);

			var value = data[i];

			buffer[index] = valid.IsSuperSetOf(new BitRange(location2, location3))
				? GetHexDigit((byte) ((value >> 4) & 0xF), uppercase)
				: invalidCellChar;

			buffer[index + 1] = valid.IsSuperSetOf(new BitRange(location1, location2))
				? GetHexDigit((byte) (value & 0xF), uppercase)
				: invalidCellChar;

			index += 2;
		}
	}

	private static void OnIsUpperCaseChanged(HexColumn arg1, AvaloniaPropertyChangedEventArgs<bool> arg2)
	{
		arg1.HexView?.InvalidateVisualLines();
	}

	private static byte? ParseNibble(char c)
	{
		return c switch
		{
			>= '0' and <= '9' => (byte?) (c - '0'),
			>= 'a' and <= 'f' => (byte?) ((c - 'a') + 10),
			>= 'A' and <= 'F' => (byte?) ((c - 'A') + 10),
			_ => null
		};
	}

	#endregion

	#region Classes

	private sealed class HexTextSource : ITextSource
	{
		#region Fields

		private readonly HexColumn _column;
		private readonly VisualBytesLine _line;
		private readonly GenericTextRunProperties _properties;

		#endregion

		#region Constructors

		public HexTextSource(HexColumn column, VisualBytesLine line, GenericTextRunProperties properties)
		{
			_column = column;
			_line = line;
			_properties = properties;
		}

		#endregion

		#region Methods

		/// <inheritdoc />
		public TextRun GetTextRun(int textSourceIndex)
		{
			// Calculate current byte location from text index.
			var byteIndex = Math.DivRem(textSourceIndex, 3, out var nibbleIndex);
			if ((byteIndex < 0) || (byteIndex >= _line.Data.Length))
			{
				return null;
			}

			// Special case nibble index 2 (space after byte).
			if (nibbleIndex == 2)
			{
				if (byteIndex >= (_line.Data.Length - 1))
				{
					return null;
				}

				return new TextCharacters(" ", _properties);
			}

			// Find current segment we're in.
			var currentLocation = new BitLocation(_line.Range.Start.ByteIndex + (ulong) byteIndex, nibbleIndex * 4);
			var segment = _line.FindSegmentContaining(currentLocation);
			if (segment is null)
			{
				return null;
			}

			// Stringify the segment.
			var range = segment.Range;
			ReadOnlySpan<byte> data = _line.AsAbsoluteSpan(range);
			Span<char> buffer = stackalloc char[((int) segment.Range.ByteLength * 3) - 1];
			_column.GetText(data, range, buffer);

			// Render
			return new TextCharacters(
				new string(buffer),
				_properties.WithBrushes(
					segment.ForegroundBrush ?? _properties.ForegroundBrush,
					segment.BackgroundBrush ?? _properties.BackgroundBrush
				)
			);
		}

		#endregion
	}

	#endregion
}