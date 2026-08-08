#region References

using Cornerstone.Collections;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers.Markdown;

[SourceReflection]
public class MarkdownParser : Parser
{
	#region Constructors

	public MarkdownParser(IStringBuffer buffer, IQueue<Block> pool) : base(buffer, pool)
	{
	}

	#endregion

	#region Methods

	public override bool IsStartCharacter()
	{
		return MarkdownTokenizer.IsStartCharacter(Buffer[Position], AtEndOfLine, AtIndentation, AtWhitespace);
	}

	protected override bool TryProcessPosition(out Block block)
	{
		var c = Buffer[Position];

		switch (c)
		{
			case '#' when AtEndOfLine && TryReadHeader(out block):
			case '*' when AtEndOfLine && TryReadHorizontalRule(out block):
			case '-' when AtEndOfLine && TryReadHorizontalRule(out block):
			case '_' when AtEndOfLine && TryReadHorizontalRule(out block):
			case '-' when AtIndentation && TryReadUnorderedList(out block):
			case '*' when AtIndentation && TryReadUnorderedList(out block):
			case '+' when AtIndentation && TryReadUnorderedList(out block):
			case '>' when AtIndentation && TryReadBlockQuote(out block):
			// Expand emphasis interiors so nested links/etc. are real blocks + Em* flags
			case '*' when AtWhitespace && TryExpandEmphasis("***", "***", bold: true, italic: true, strike: false, out block):
			case '_' when AtWhitespace && TryExpandEmphasis("___", "___", bold: true, italic: true, strike: false, out block):
			case '*' when AtWhitespace && TryExpandEmphasis("**", "**", bold: true, italic: false, strike: false, out block):
			case '_' when AtWhitespace && TryExpandEmphasis("__", "__", bold: true, italic: false, strike: false, out block):
			case '*' when AtWhitespace && TryExpandEmphasis("*", "*", bold: false, italic: true, strike: false, out block):
			case '_' when AtWhitespace && TryExpandEmphasis("_", "_", bold: false, italic: true, strike: false, out block):
			case '~' when AtIndentation && TryReadFencedCodeBlock(out block):
			case '`' when AtIndentation && TryReadFencedCodeBlock(out block):
			case '`' when AtWhitespace && TryProcessDelimitedInlineSelection('`', MarkdownTokenizer.TokenTypeInlineCode, out block):
			case '~' when AtWhitespace && TryExpandEmphasis("~~", "~~", bold: false, italic: false, strike: true, out block):
			case '[' when TryReadLink(out block):
			case '|' when AtEndOfLine && TryReadTable(out block):
			{
				return true;
			}
			default:
			{
				block = ReadText();
				return true;
			}
		}
	}

	/// <summary>
	/// Matches open/close delimiters, parses the interior as real blocks (with Em* flags),
	/// and returns the first interior block (rest via <see cref="TextProcessor{T}.EnqueuePending"/>).
	/// </summary>
	private bool TryExpandEmphasis(string open, string close, bool bold, bool italic, bool strike, out Block first)
	{
		first = null;
		if (!TryMatch(Position, open))
		{
			return false;
		}

		var openStart = Position;
		var contentStart = openStart + open.Length;
		var contentEnd = FindClosingDelimiter(contentStart, close);
		if (contentEnd < 0)
		{
			return false;
		}

		var afterClose = contentEnd + close.Length;
		var length = contentEnd - contentStart;

		// Empty emphasis
		if (length <= 0)
		{
			return false;
		}

		// Parse interior in a nested parser so structure (links, nested emphasis) is accurate.
		// Outer Em* flags are OR'd onto leaves after parse (StartProcessing resets nested depths).
		var content = Buffer.Substring(contentStart, length);
		var nested = new MarkdownParser(new StringBuffer(content), Pool);

		Block firstInner = null;
		foreach (var inner in nested.Process())
		{
			RemapBlockToParent(inner, contentStart);
			if (bold)
			{
				inner.EmBold = true;
			}
			if (italic)
			{
				inner.EmItalic = true;
			}
			if (strike)
			{
				inner.EmStrikethrough = true;
			}

			if (firstInner is null)
			{
				firstInner = inner;
			}
			else
			{
				EnqueuePending(inner);
			}
		}

		if (firstInner is null)
		{
			// Interior produced nothing — fall back to a single styled text span
			first = CreateOrUpdateSection(TextProcessor.TokenTypeText, contentStart, contentEnd);
			if (bold)
			{
				first.EmBold = true;
			}
			if (italic)
			{
				first.EmItalic = true;
			}
			if (strike)
			{
				first.EmStrikethrough = true;
			}
		}
		else
		{
			first = firstInner;
		}

		Position = afterClose;
		CurrentState = LexerStateDefault;
		return true;
	}

	private int FindClosingDelimiter(int searchFrom, string close)
	{
		var position = searchFrom;
		while (position < Buffer.Count)
		{
			if (TryMatch(position, close))
			{
				return position;
			}
			position++;
		}
		return -1;
	}

	private static void RemapBlockToParent(Block block, int contentStart)
	{
		block.StartOffset += contentStart;
		block.EndOffset += contentStart;
		if (block.Offsets is { Length: > 0 })
		{
			var mapped = new int[block.Offsets.Length];
			for (var i = 0; i < block.Offsets.Length; i++)
			{
				mapped[i] = block.Offsets[i] + contentStart;
			}
			block.Offsets = mapped;
		}
	}

	/// <summary>
	/// Reads <c>[text](destination)</c>.
	/// Offsets: [textStart, textEnd, destinationStart, destinationEnd].
	/// </summary>
	private bool TryReadLink(out Block block)
	{
		if (!MarkdownLink.TryRead(Buffer, Position,
			    out var start, out var end,
			    out var textStart, out var textEnd,
			    out var destinationStart, out var destinationEnd))
		{
			block = null;
			return false;
		}

		block = CreateOrUpdateSection(
			MarkdownTokenizer.TokenTypeLink,
			start,
			end,
			offsets: [textStart, textEnd, destinationStart, destinationEnd]
		);
		Position = end;
		return true;
	}

	/// <summary>
	/// Reads a fenced code block (``` or ~~~). Supports incomplete fences for streaming:
	/// if no closer is present yet, the block spans to EOF and remains TokenTypeCodeBlock.
	/// </summary>
	private bool TryReadFencedCodeBlock(out Block block)
	{
		if (!MarkdownFence.TryRead(Buffer, Position, out var fence))
		{
			block = null;
			return false;
		}

		block = CreateOrUpdateSection(
			MarkdownTokenizer.TokenTypeCodeBlock,
			fence.StartOffset,
			fence.EndOffset,
			offsets: [fence.ContentRegionStart, fence.ContentRegionEnd]
		);
		Position = fence.EndOffset;
		return true;
	}

	private bool TryReadBlockQuote(out Block block)
	{
		block = null;
		var start = Position;
		var end = CalculatePastIndentation(start);

		if ((end >= Buffer.Count) || (Buffer[end] != '>'))
		{
			return false;
		}

		// Calculate to the rest of the line (the actual quote content)
		end = CalculateUntilEndOfLine(end);

		// Block quotes can span multiple lines.
		// Continue consuming subsequent lines that start with '>' (with optional indent)
		var nextStart = end;
		while (nextStart < Buffer.Count)
		{
			// consumes end of line and indentation
			nextStart = CalculatePastEndOfLine(nextStart);
			nextStart = CalculatePastIndentation(nextStart);

			if ((nextStart >= Buffer.Count)
				|| (Buffer[nextStart] != '>'))
			{
				break;
			}

			// Consume the rest of the line (the actual quote content)
			nextStart = CalculateUntilEndOfLine(nextStart);
			end = nextStart;
		}

		block = CreateOrUpdateSection(MarkdownTokenizer.TokenTypeBlockQuote, start, end);
		Position = end;
		return true;
	}

	private bool TryReadHeader(out Block block)
	{
		// skip the first, then remaining # header token
		var start = Position;
		Position++;
		var headerOffset = ConsumeCharacters('#');
		var whitespaceOffset = ConsumeWhitespace();
		ConsumeRestOfLine();
		block = CreateOrUpdateSection(MarkdownTokenizer.TokenTypeHeader, start, Position, offsets: [headerOffset, whitespaceOffset]);
		return true;
	}

	/// <summary>
	/// A horizontal rule, use three or more asterisks(***),
	/// dashes(---), or underscores(___) on a line by themselves.
	/// </summary>
	private bool TryReadHorizontalRule(out Block block)
	{
		var start = Position;
		var c = Buffer[Position];
		var endOfLine = CalculateUntilEndOfLine(Position);
		var lastCharacter = CalculateUntilNot(start, c);
		var length = endOfLine - start;
		if ((length >= 3) && (endOfLine == lastCharacter))
		{
			Position = endOfLine;
			block = CreateOrUpdateSection(MarkdownTokenizer.TokenTypeHorizontalRule, start, Position);
			return true;
		}

		block = null;
		return false;
	}

	/// <summary>
	/// A Markdown table (GFM style):
	/// | Name          | Age | City      |
	/// |---------------|-----|-----------|
	/// | Alice         |  30 | New York  |
	/// | Bob           |  25 | San Fran  |
	/// 
	/// Consumes the entire table as a single TokenTypeTable token.
	/// Falls back to normal text if there is no separator row.
	/// </summary>
	private bool TryReadTable(out Block block)
	{
		block = null;
		var tableStart = Position;

		if (Buffer[Position] != '|')
		{
			return false;
		}

		// 1. Validate header row (must have at least one inner pipe)
		var lineEnd = CalculateUntilEndOfLine(Position);
		var hasInnerPipe = false;
		for (var i = tableStart + 1; i < lineEnd; i++)
		{
			if (Buffer[i] == '|')
			{
				hasInnerPipe = true;
				break;
			}
		}
		if (!hasInnerPipe)
		{
			return false;
		}

		var savedPosition = Position;

		// Consume header row (moves to start of next line)
		ConsumeRestOfLine();

		// 2. Must have a separator row next
		if ((Position >= Buffer.Count) || (Buffer[Position - 1] != '|'))
		{
			Position = savedPosition;
			return false;
		}

		// Validate separator row: contains at least one '-', only | : - whitespace allowed
		ConsumeNewLines();
		lineEnd = CalculateUntilEndOfLine(Position);
		var hasDash = false;
		var isValidSeparator = true;
		for (var i = Position; i < lineEnd; i++)
		{
			var c = Buffer[i];
			if (c == '-')
			{
				hasDash = true;
			}
			else if ((c != '|') && (c != ':') && !char.IsWhiteSpace(c))
			{
				isValidSeparator = false;
				break;
			}
		}

		if (!hasDash || !isValidSeparator)
		{
			Position = savedPosition;
			return false;
		}

		// Consume separator row
		ConsumeRestOfLine();
		ConsumeNewLines();

		// 3. Consume all following data rows that look like table rows
		while ((Position < Buffer.Count) && (Buffer[Position] == '|'))
		{
			lineEnd = CalculateUntilEndOfLine(Position);
			hasInnerPipe = false;
			for (var i = Position + 1; i < lineEnd; i++)
			{
				if (Buffer[i] == '|')
				{
					hasInnerPipe = true;
					break;
				}
			}
			if (!hasInnerPipe)
			{
				break;
			}

			ConsumeRestOfLine();
			ConsumeNewLines();
		}

		block = CreateOrUpdateSection(MarkdownTokenizer.TokenTypeTable, tableStart, Position);
		return true;
	}

	/// <summary>
	/// Parses unordered list items (-, *, +) followed by whitespace.
	/// Consumes consecutive lines that start with a valid list marker (respecting indentation).
	/// </summary>
	private bool TryReadUnorderedList(out Block block)
	{
		block = null;
		var blockStart = Position;
		var start = CalculatePastIndentation(blockStart);

		// Only process if it's a valid list marker
		var c = Buffer[start];
		if ((c != '-') && (c != '*') && (c != '+'))
		{
			return false;
		}

		// Must be followed by at least one whitespace character
		if ((++start >= Buffer.Count)
			|| !char.IsWhiteSpace(Buffer[start]))
		{
			return false;
		}

		// Consume marker and following whitespace
		start = CalculatePastWhitespace(start);
		var blockEnd = CalculateUntilEndOfLine(start);

		// Continue consuming subsequent lines that start with a list marker
		start = CalculatePastEndOfLine(blockEnd);
		while (start < Buffer.Count)
		{
			start = CalculatePastIndentation(start);

			// Only process if it's a valid list marker
			c = Buffer[start];
			if ((c != '-') && (c != '*') && (c != '+'))
			{
				break;
			}

			// Must be followed by at least one whitespace character
			if ((++start >= Buffer.Count)
				|| !char.IsWhiteSpace(Buffer[start]))
			{
				break;
			}

			// Consume marker and following whitespace
			start = CalculatePastWhitespace(start);
			blockEnd = CalculateUntilEndOfLine(start);

			// Move to the next line
			start = CalculatePastEndOfLine(blockEnd);
		}

		block = CreateOrUpdateSection(MarkdownTokenizer.TokenTypeUnorderedList, blockStart, blockEnd);
		Position = blockEnd;
		return true;
	}

	#endregion
}