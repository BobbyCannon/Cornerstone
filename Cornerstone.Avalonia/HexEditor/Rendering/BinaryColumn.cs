#region References

using System;
using Avalonia;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.HexEditor.Document;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

/// <summary>
/// Represents a column that renders binary data using the binary number encoding.
/// </summary>
public class BinaryColumn : CellBasedColumn
{
	#region Constructors

	static BinaryColumn()
	{
		CursorProperty.OverrideDefaultValue<BinaryColumn>(IBeamCursor);
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override int BitsPerCell => 1;

	/// <inheritdoc />
	public override int CellsPerWord => 8;

	/// <inheritdoc />
	public override double GroupPadding => CellSize.Width;

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
			new BinaryTextSource(this, line, properties),
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
		if (ParseBit(input) is not { } bit)
		{
			return false;
		}

		var relativeByteIndex = (int) (writeLocation.ByteIndex - bufferStart.ByteIndex);
		buffer[relativeByteIndex] = (byte) (
			(buffer[relativeByteIndex] & ~(1 << writeLocation.BitIndex)) | (bit << writeLocation.BitIndex)
		);

		return true;
	}

	private void GetText(ReadOnlySpan<byte> data, BitRange dataRange, Span<char> buffer)
	{
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

			var value = data[i];

			for (var j = 0; j < 8; j++)
			{
				var location = new BitLocation(dataRange.Start.ByteIndex + (ulong) i, 7 - j);
				buffer[index + j] = valid.Contains(location)
					? (char) (((value >> location.BitIndex) & 1) + '0')
					: invalidCellChar;
			}

			index += 8;
		}
	}

	private static byte? ParseBit(char c)
	{
		return c switch
		{
			'0' => 0,
			'1' => 1,
			_ => null
		};
	}

	#endregion

	#region Classes

	private sealed class BinaryTextSource : ITextSource
	{
		#region Fields

		private readonly BinaryColumn _column;
		private readonly VisualBytesLine _line;
		private readonly GenericTextRunProperties _properties;

		#endregion

		#region Constructors

		public BinaryTextSource(BinaryColumn column, VisualBytesLine line, GenericTextRunProperties properties)
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
			var byteIndex = Math.DivRem(textSourceIndex, 9, out var bitIndex);
			if ((byteIndex < 0) || (byteIndex >= _line.Data.Length))
			{
				return null;
			}

			// Special case nibble index 8 (space after byte).
			if (bitIndex == 8)
			{
				if (byteIndex >= (_line.Data.Length - 1))
				{
					return null;
				}

				return new TextCharacters(" ", _properties);
			}

			// Find current segment we're in.
			var currentLocation = new BitLocation(_line.Range.Start.ByteIndex + (ulong) byteIndex, bitIndex);
			var segment = _line.FindSegmentContaining(currentLocation);
			if (segment is null)
			{
				return null;
			}

			// Stringify the segment.
			var range = segment.Range;
			ReadOnlySpan<byte> data = _line.AsAbsoluteSpan(range);
			Span<char> buffer = stackalloc char[((int) segment.Range.ByteLength * 9) - 1];
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