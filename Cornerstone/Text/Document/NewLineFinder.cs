#region References

using System;
using System.Text;

#endregion

namespace Cornerstone.Text.Document;

internal static class NewLineFinder
{
	#region Fields

	internal static readonly string[] NewlineStrings = ["\r\n", "\r", "\n"];
	private static readonly char[] Newline = ['\r', '\n'];

	#endregion

	#region Methods

	/// <summary>
	/// Gets the location of the next new line character, or SegmentExtensions.Invalid
	/// if none is found.
	/// </summary>
	internal static SimpleSegment NextNewLine(string text, int offset)
	{
		var pos = text.IndexOfAny(Newline, offset);
		if (pos >= 0)
		{
			if (text[pos] == '\r')
			{
				if (((pos + 1) < text.Length) && (text[pos + 1] == '\n'))
				{
					return new SimpleSegment(pos, 2);
				}
			}
			return new SimpleSegment(pos, 1);
		}
		return SegmentExtensions.Invalid;
	}

	/// <summary>
	/// Gets the location of the next new line character, or SegmentExtensions.Invalid
	/// if none is found.
	/// </summary>
	internal static SimpleSegment NextNewLine(ITextSource text, int offset)
	{
		var textLength = text.TextLength;
		var pos = text.IndexOfAny(Newline, offset, textLength - offset);
		if (pos >= 0)
		{
			if (text.GetCharAt(pos) == '\r')
			{
				if (((pos + 1) < textLength) && (text.GetCharAt(pos + 1) == '\n'))
				{
					return new SimpleSegment(pos, 2);
				}
			}
			return new SimpleSegment(pos, 1);
		}
		return SegmentExtensions.Invalid;
	}

	#endregion
}

partial class TextUtilities
{
	#region Methods

	/// <summary>
	/// Finds the next new line character starting at offset.
	/// </summary>
	/// <param name="text"> The text source to search in. </param>
	/// <param name="offset"> The starting offset for the search. </param>
	/// <param name="newLineType"> The string representing the new line that was found, or null if no new line was found. </param>
	/// <returns>
	/// The position of the first new line starting at or after <paramref name="offset" />,
	/// or -1 if no new line was found.
	/// </returns>
	public static int FindNextNewLine(ITextSource text, int offset, out string newLineType)
	{
		if (text == null)
		{
			throw new ArgumentNullException(nameof(text));
		}
		if ((offset < 0) || (offset > text.TextLength))
		{
			throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset is outside of text source");
		}
		var s = NewLineFinder.NextNewLine(text, offset);
		if (s == SegmentExtensions.Invalid)
		{
			newLineType = null;
			return -1;
		}
		if (s.Length == 2)
		{
			newLineType = "\r\n";
		}
		else if (text.GetCharAt(s.Offset) == '\n')
		{
			newLineType = "\n";
		}
		else
		{
			newLineType = "\r";
		}
		return s.Offset;
	}

	/// <summary>
	/// Gets the newline sequence used in the document at the specified line.
	/// </summary>
	public static string GetNewLineFromDocument(IDocument document, int lineNumber)
	{
		var line = document.GetLineByNumber(lineNumber);
		if (line.DelimiterLength == 0)
		{
			// at the end of the document, there's no line delimiter, so use the delimiter
			// from the previous line
			line = line.PreviousLine;
			if (line == null)
			{
				return Environment.NewLine;
			}
		}
		return document.GetText(line.Offset + line.Length, line.DelimiterLength);
	}

	/// <summary>
	/// Gets whether the specified string is a newline sequence.
	/// </summary>
	public static bool IsNewLine(string newLine)
	{
		return (newLine == "\r\n") || (newLine == "\n") || (newLine == "\r");
	}

	/// <summary>
	/// Normalizes all new lines in <paramref name="input" /> to be <paramref name="newLine" />.
	/// </summary>
	public static string NormalizeNewLines(string input, string newLine)
	{
		if (input == null)
		{
			return null;
		}
		if (!IsNewLine(newLine))
		{
			throw new ArgumentException("newLine must be one of the known newline sequences");
		}
		var ds = NewLineFinder.NextNewLine(input, 0);
		if (ds == SegmentExtensions.Invalid) // text does not contain any new lines
		{
			return input;
		}
		var b = new StringBuilder(input.Length);
		var lastEndOffset = 0;
		do
		{
			b.Append(input, lastEndOffset, ds.Offset - lastEndOffset);
			b.Append(newLine);
			lastEndOffset = ds.EndOffset;
			ds = NewLineFinder.NextNewLine(input, lastEndOffset);
		} while (ds != SegmentExtensions.Invalid);
		// remaining string (after last newline)
		b.Append(input, lastEndOffset, input.Length - lastEndOffset);
		return b.ToString();
	}

	#endregion
}