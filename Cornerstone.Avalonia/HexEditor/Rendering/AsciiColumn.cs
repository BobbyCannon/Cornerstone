#region References

using System;
using Avalonia;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.HexEditor.Document;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

/// <summary>
/// Represents a column that renders binary data using the ASCII text encoding.
/// </summary>
public class AsciiColumn : CellBasedColumn
{
	#region Constructors

	static AsciiColumn()
	{
		CursorProperty.OverrideDefaultValue<AsciiColumn>(IBeamCursor);
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override int BitsPerCell => 8;

	/// <inheritdoc />
	public override int CellsPerWord => 1;

	/// <inheritdoc />
	public override double GroupPadding => 0;

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
			new AsciiTextSource(this, line, properties),
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

		var output = new char[data.Length];
		GetText(data, range, output);

		return new string(output);
	}

	/// <inheritdoc />
	protected override bool TryWriteCell(Span<byte> buffer, BitLocation bufferStart, BitLocation writeLocation, char input)
	{
		buffer[(int) (writeLocation.ByteIndex - bufferStart.ByteIndex)] = (byte) input;
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

		for (var i = 0; i < data.Length; i++)
		{
			var cellLocation = new BitLocation(dataRange.Start.ByteIndex + (ulong) i, 0);
			var cellRange = new BitRange(cellLocation, cellLocation.AddBits(8));

			buffer[i] = valid.IsSuperSetOf(cellRange)
				? MapToPrintableChar(data[i])
				: invalidCellChar;
		}
	}

	private static char MapToPrintableChar(byte b)
	{
		return b switch
		{
			>= 0x20 and < 0x7f => (char) b,
			_ => '.'
		};
	}

	#endregion

	#region Classes

	private sealed class AsciiTextSource : ITextSource
	{
		#region Fields

		private readonly AsciiColumn _column;
		private readonly VisualBytesLine _line;
		private readonly GenericTextRunProperties _properties;

		#endregion

		#region Constructors

		public AsciiTextSource(AsciiColumn column, VisualBytesLine line, GenericTextRunProperties properties)
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
			// Find current segment we're in.
			var currentLocation = new BitLocation(_line.Range.Start.ByteIndex + (ulong) textSourceIndex);
			var segment = _line.FindSegmentContaining(currentLocation);
			if (segment is null)
			{
				return null;
			}

			// Stringify the segment.
			var range = segment.Range;
			ReadOnlySpan<byte> data = _line.AsAbsoluteSpan(range);
			Span<char> buffer = stackalloc char[(int) range.ByteLength];
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