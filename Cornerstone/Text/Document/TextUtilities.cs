#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Text.Document;

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
	public static string GetNewLineFromDocument(ITextEditorDocument document, int lineNumber)
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
		return document.GetText(line.StartIndex + line.Length, line.DelimiterLength);
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
			lastEndOffset = ds.EndIndex;
			ds = NewLineFinder.NextNewLine(input, lastEndOffset);
		} while (ds != SegmentExtensions.Invalid);
		// remaining string (after last newline)
		b.Append(input, lastEndOffset, input.Length - lastEndOffset);
		return b.ToString();
	}

	#endregion
}

public enum LogicalDirection
{
	Backward,
	Forward
}

/// <summary>
/// Specifies the mode for getting the next caret position.
/// </summary>
public enum CaretPositioningMode
{
	/// <summary>
	/// Normal positioning (stop after every grapheme)
	/// </summary>
	Normal,

	/// <summary>
	/// Stop only on word borders.
	/// </summary>
	WordBorder,

	/// <summary>
	/// Stop only at the beginning of words. This is used for Ctrl+Left/Ctrl+Right.
	/// </summary>
	WordStart,

	/// <summary>
	/// Stop only at the beginning of words, and anywhere in the middle of symbols.
	/// </summary>
	WordStartOrSymbol,

	/// <summary>
	/// Stop only on word borders, and anywhere in the middle of symbols.
	/// </summary>
	WordBorderOrSymbol,

	/// <summary>
	/// Stop between every Unicode codepoint, even within the same grapheme.
	/// This is used to implement deleting the previous grapheme when Backspace is pressed.
	/// </summary>
	EveryCodepoint
}

/// <summary>
/// Static helper methods for working with text.
/// </summary>
public static partial class TextUtilities
{
	#region Fields

	// the names of the first 32 ASCII characters = Unicode C0 block
	private static readonly string[] C0Table =
	[
		"NUL", "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL", "BS", "HT",
		"LF", "VT", "FF", "CR", "SO", "SI", "DLE", "DC1", "DC2", "DC3",
		"DC4", "NAK", "SYN", "ETB", "CAN", "EM", "SUB", "ESC", "FS", "GS",
		"RS", "US"
	];

	// DEL (ASCII 127) and
	// the names of the control characters in the C1 block (Unicode 128 to 159)
	private static readonly string[] DelAndC1Table =
	[
		"DEL",
		"PAD", "HOP", "BPH", "NBH", "IND", "NEL", "SSA", "ESA", "HTS", "HTJ",
		"VTS", "PLD", "PLU", "RI", "SS2", "SS3", "DCS", "PU1", "PU2", "STS",
		"CCH", "MW", "SPA", "EPA", "SOS", "SGCI", "SCI", "CSI", "ST", "OSC",
		"PM", "APC"
	];

	private static readonly Func<char, UnicodeCategory> GetUnicodeCategory = (Func<char, UnicodeCategory>) typeof(char)
		.GetRuntimeMethod("GetUnicodeCategory", [typeof(char)])
		.CreateDelegate(typeof(Func<char, UnicodeCategory>));

	private static readonly Func<string, int, UnicodeCategory> GetUnicodeCategoryString = (Func<string, int, UnicodeCategory>) typeof(char)
		.GetRuntimeMethod("GetUnicodeCategory", [typeof(string), typeof(int)])
		.CreateDelegate(typeof(Func<string, int, UnicodeCategory>));

	#endregion

	#region Methods

	/// <summary>
	/// Gets whether the character is whitespace, part of an identifier, or line terminator.
	/// </summary>
	[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "c")]
	public static CharacterClass GetCharacterClass(char c)
	{
		if ((c == '\r') || (c == '\n'))
		{
			return CharacterClass.LineTerminator;
		}
		if (c == '_')
		{
			return CharacterClass.IdentifierPart;
		}
		return GetCharacterClass(GetUnicodeCategory(c));
	}

	/// <summary>
	/// Gets the name of the control character.
	/// For unknown characters, the unicode codepoint is returned as 4-digit hexadecimal value.
	/// </summary>
	public static string GetControlCharacterName(char controlCharacter)
	{
		int num = controlCharacter;
		if (num < C0Table.Length)
		{
			return C0Table[num];
		}
		if ((num >= 127) && (num <= 159))
		{
			return DelAndC1Table[num - 127];
		}
		return num.ToString("x4", CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Gets the leading whitespace segment on the document line.
	/// </summary>
	[SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace")]
	[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
		Justification = "Parameter cannot be ITextSource because it must belong to the DocumentLine")]
	public static IRange GetLeadingWhitespace(TextEditorDocument document, DocumentLine documentLine)
	{
		if (documentLine == null)
		{
			throw new ArgumentNullException(nameof(documentLine));
		}
		return GetWhitespaceAfter(document, documentLine.StartIndex);
	}

	/// <summary>
	/// Gets the next caret position.
	/// </summary>
	/// <param name="textSource"> The text source. </param>
	/// <param name="offset"> The start offset inside the text source. </param>
	/// <param name="direction"> The search direction (forwards or backwards). </param>
	/// <param name="mode"> The mode for caret positioning. </param>
	/// <returns>
	/// The offset of the next caret position, or -1 if there is no further caret position
	/// in the text source.
	/// </returns>
	/// <remarks>
	/// This method is NOT equivalent to the actual caret movement when using VisualLine.GetNextCaretPosition.
	/// In real caret movement, there are additional caret stops at line starts and ends. This method
	/// treats linefeeds as simple whitespace.
	/// </remarks>
	public static int GetNextCaretPosition(ITextSource textSource, int offset, LogicalDirection direction, CaretPositioningMode mode)
	{
		if (textSource == null)
		{
			throw new ArgumentNullException(nameof(textSource));
		}
		switch (mode)
		{
			case CaretPositioningMode.Normal:
			case CaretPositioningMode.EveryCodepoint:
			case CaretPositioningMode.WordBorder:
			case CaretPositioningMode.WordBorderOrSymbol:
			case CaretPositioningMode.WordStart:
			case CaretPositioningMode.WordStartOrSymbol:
				break; // OK
			default:
				throw new ArgumentException("Unsupported CaretPositioningMode: " + mode, nameof(mode));
		}
		if ((direction != LogicalDirection.Backward)
			&& (direction != LogicalDirection.Forward))
		{
			throw new ArgumentException("Invalid LogicalDirection: " + direction, nameof(direction));
		}
		var textLength = textSource.TextLength;
		if (textLength <= 0)
		{
			// empty document? has a normal caret position at 0, though no word borders
			if (IsNormal(mode))
			{
				if ((offset > 0) && (direction == LogicalDirection.Backward))
				{
					return 0;
				}
				if ((offset < 0) && (direction == LogicalDirection.Forward))
				{
					return 0;
				}
			}
			return -1;
		}
		while (true)
		{
			var nextPos = direction == LogicalDirection.Backward ? offset - 1 : offset + 1;

			// return -1 if there is no further caret position in the text source
			// we also need this to handle offset values outside the valid range
			if ((nextPos < 0) || (nextPos > textLength))
			{
				return -1;
			}

			// check if we've run against the textSource borders.
			// a 'textSource' usually isn't the whole document, but a single VisualLineElement.
			if (nextPos == 0)
			{
				// at the document start, there's only a word border
				// if the first character is not whitespace
				if (IsNormal(mode) || !char.IsWhiteSpace(textSource.GetCharAt(0)))
				{
					return nextPos;
				}
			}
			else if (nextPos == textLength)
			{
				// at the document end, there's never a word start
				if ((mode != CaretPositioningMode.WordStart) && (mode != CaretPositioningMode.WordStartOrSymbol))
				{
					// at the document end, there's only a word border
					// if the last character is not whitespace
					if (IsNormal(mode) || !char.IsWhiteSpace(textSource.GetCharAt(textLength - 1)))
					{
						return nextPos;
					}
				}
			}
			else
			{
				var charBefore = textSource.GetCharAt(nextPos - 1);
				var charAfter = textSource.GetCharAt(nextPos);
				// Don't stop in the middle of a surrogate pair
				if (!char.IsSurrogatePair(charBefore, charAfter))
				{
					var classBefore = GetCharacterClass(charBefore);
					var classAfter = GetCharacterClass(charAfter);
					// get correct class for characters outside BMP:
					if (char.IsLowSurrogate(charBefore) && (nextPos >= 2))
					{
						classBefore = GetCharacterClass(textSource.GetCharAt(nextPos - 2), charBefore);
					}
					if (char.IsHighSurrogate(charAfter) && ((nextPos + 1) < textLength))
					{
						classAfter = GetCharacterClass(charAfter, textSource.GetCharAt(nextPos + 1));
					}
					if (StopBetweenCharacters(mode, classBefore, classAfter))
					{
						return nextPos;
					}
				}
			}
			// we'll have to continue searching...
			offset = nextPos;
		}
	}

	/// <summary>
	/// Gets a single indentation segment starting at <paramref name="offset" /> - at most one tab
	/// or <paramref name="indentationSize" /> spaces.
	/// </summary>
	/// <param name="textSource"> The text source. </param>
	/// <param name="offset"> The offset where the indentation segment starts. </param>
	/// <param name="indentationSize"> The size of an indentation unit. See <see cref="TextEditorOptions.IndentationSize" />. </param>
	/// <returns>
	/// The indentation segment.
	/// If there is no indentation character at the specified <paramref name="offset" />,
	/// an empty segment is returned.
	/// </returns>
	public static IRange GetSingleIndentationSegment(ITextSource textSource, int offset, int indentationSize)
	{
		if (textSource == null)
		{
			throw new ArgumentNullException(nameof(textSource));
		}
		var pos = offset;
		while (pos < textSource.TextLength)
		{
			var c = textSource.GetCharAt(pos);
			if (c == '\t')
			{
				if (pos == offset)
				{
					return new SimpleRange(offset, 1);
				}
				break;
			}
			if (c == ' ')
			{
				if ((pos - offset) >= indentationSize)
				{
					break;
				}
			}
			else
			{
				break;
			}
			// continue only if c==' ' and (pos-offset)<tabSize
			pos++;
		}
		return new SimpleRange(offset, pos - offset);
	}

	/// <summary>
	/// Gets the trailing whitespace segment on the document line.
	/// </summary>
	[SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace")]
	[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
		Justification = "Parameter cannot be ITextSource because it must belong to the DocumentLine")]
	public static IRange GetTrailingWhitespace(TextEditorDocument document, DocumentLine documentLine)
	{
		if (documentLine == null)
		{
			throw new ArgumentNullException(nameof(documentLine));
		}
		var segment = GetWhitespaceBefore(document, documentLine.EndIndex);
		// If the whole line consists of whitespace, we consider all of it as leading whitespace,
		// so return an empty segment as trailing whitespace.
		if (segment.StartIndex == documentLine.StartIndex)
		{
			return new SimpleRange(documentLine.EndIndex, 0);
		}
		return segment;
	}

	/// <summary>
	/// Gets all whitespace (' ' and '\t', but no newlines) after offset.
	/// </summary>
	/// <param name="textSource"> The text source. </param>
	/// <param name="offset"> The offset where the whitespace starts. </param>
	/// <returns> The segment containing the whitespace. </returns>
	[SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace")]
	public static IRange GetWhitespaceAfter(ITextSource textSource, int offset)
	{
		if (textSource == null)
		{
			throw new ArgumentNullException(nameof(textSource));
		}
		int pos;
		for (pos = offset; pos < textSource.TextLength; pos++)
		{
			var c = textSource.GetCharAt(pos);
			if ((c != ' ') && (c != '\t'))
			{
				break;
			}
		}
		return new SimpleRange(offset, pos - offset);
	}

	/// <summary>
	/// Gets all whitespace (' ' and '\t', but no newlines) before offset.
	/// </summary>
	/// <param name="textSource"> The text source. </param>
	/// <param name="offset"> The offset where the whitespace ends. </param>
	/// <returns> The segment containing the whitespace. </returns>
	[SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace")]
	public static IRange GetWhitespaceBefore(ITextSource textSource, int offset)
	{
		if (textSource == null)
		{
			throw new ArgumentNullException(nameof(textSource));
		}
		int pos;
		for (pos = offset - 1; pos >= 0; pos--)
		{
			var c = textSource.GetCharAt(pos);
			if ((c != ' ') && (c != '\t'))
			{
				break;
			}
		}
		pos++; // go back the one character that isn't whitespace
		return new SimpleRange(pos, offset - pos);
	}

	private static CharacterClass GetCharacterClass(char highSurrogate, char lowSurrogate)
	{
		if (char.IsSurrogatePair(highSurrogate, lowSurrogate))
		{
			return GetCharacterClass(GetUnicodeCategoryString(highSurrogate.ToString() + lowSurrogate, 0));
		}
		// malformed surrogate pair
		return CharacterClass.Other;
	}

	private static CharacterClass GetCharacterClass(UnicodeCategory c)
	{
		switch (c)
		{
			case UnicodeCategory.SpaceSeparator:
			case UnicodeCategory.LineSeparator:
			case UnicodeCategory.ParagraphSeparator:
			case UnicodeCategory.Control:
				return CharacterClass.Whitespace;
			case UnicodeCategory.UppercaseLetter:
			case UnicodeCategory.LowercaseLetter:
			case UnicodeCategory.TitlecaseLetter:
			case UnicodeCategory.ModifierLetter:
			case UnicodeCategory.OtherLetter:
			case UnicodeCategory.DecimalDigitNumber:
				return CharacterClass.IdentifierPart;
			case UnicodeCategory.NonSpacingMark:
			case UnicodeCategory.SpacingCombiningMark:
			case UnicodeCategory.EnclosingMark:
				return CharacterClass.CombiningMark;
			default:
				return CharacterClass.Other;
		}
	}

	private static bool IsNormal(CaretPositioningMode mode)
	{
		return (mode == CaretPositioningMode.Normal) || (mode == CaretPositioningMode.EveryCodepoint);
	}

	private static bool StopBetweenCharacters(CaretPositioningMode mode, CharacterClass charBefore, CharacterClass charAfter)
	{
		if (mode == CaretPositioningMode.EveryCodepoint)
		{
			return true;
		}
		// Don't stop in the middle of a grapheme
		if (charAfter == CharacterClass.CombiningMark)
		{
			return false;
		}
		// Stop after every grapheme in normal mode
		if (mode == CaretPositioningMode.Normal)
		{
			return true;
		}
		if (charBefore == charAfter)
		{
			if ((charBefore == CharacterClass.Other) &&
				((mode == CaretPositioningMode.WordBorderOrSymbol) || (mode == CaretPositioningMode.WordStartOrSymbol)))
			{
				// With the "OrSymbol" modes, there's a word border and start between any two unknown characters
				return true;
			}
		}
		else
		{
			// this looks like a possible border

			// if we're looking for word starts, check that this is a word start (and not a word end)
			// if we're just checking for word borders, accept unconditionally
			if (!(((mode == CaretPositioningMode.WordStart) || (mode == CaretPositioningMode.WordStartOrSymbol))
					&& ((charAfter == CharacterClass.Whitespace) || (charAfter == CharacterClass.LineTerminator))))
			{
				return true;
			}
		}
		return false;
	}

	#endregion
}

/// <summary>
/// Classifies a character as whitespace, line terminator, part of an identifier, or other.
/// </summary>
public enum CharacterClass
{
	/// <summary>
	/// The character is not whitespace, line terminator or part of an identifier.
	/// </summary>
	Other,

	/// <summary>
	/// The character is whitespace (but not line terminator).
	/// </summary>
	[SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace")]
	Whitespace,

	/// <summary>
	/// The character can be part of an identifier (Letter, digit or underscore).
	/// </summary>
	IdentifierPart,

	/// <summary>
	/// The character is line terminator (\r or \n).
	/// </summary>
	LineTerminator,

	/// <summary>
	/// The character is a unicode combining mark that modifies the previous character.
	/// Corresponds to the Unicode designations "Mn", "Mc" and "Me".
	/// </summary>
	CombiningMark
}