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

	public override bool TryProcessPosition(out Block block)
	{
		var c = Buffer[Position];

		switch (c)
		{
			case '#' when AtEndOfLine:
			{
				block = ReadHeader();
				return true;
			}
			case '>' when AtIndentation && TryReadBlockQuote(out block):
			case '*' when AtEndOfLine && TryReadHorizontalRule(out block):
			case '-' when AtEndOfLine && TryReadHorizontalRule(out block):
			case '_' when AtEndOfLine && TryReadHorizontalRule(out block):
			case '*' when AtWhitespace && TryProcessDelimitedBlock("***", "***", MarkdownTokenizer.TokenTypeBoldAndItalic, out block):
			case '_' when AtWhitespace && TryProcessDelimitedBlock("___", "___", MarkdownTokenizer.TokenTypeBoldAndItalic, out block):
			case '*' when AtWhitespace && TryProcessDelimitedBlock("**", "**", MarkdownTokenizer.TokenTypeBold, out block):
			case '_' when AtWhitespace && TryProcessDelimitedBlock("__", "__", MarkdownTokenizer.TokenTypeBold, out block):
			case '*' when AtWhitespace && TryProcessDelimitedBlock("*", "*", MarkdownTokenizer.TokenTypeItalic, out block):
			case '_' when AtWhitespace && TryProcessDelimitedBlock("_", "_", MarkdownTokenizer.TokenTypeItalic, out block):
			case '~' when AtWhitespace && TryProcessDelimitedBlock("~~", "~~", MarkdownTokenizer.TokenTypeStrikethrough, out block):
			case '`' when AtEndOfLine && TryProcessDelimitedBlock("```", "```", MarkdownTokenizer.TokenTypeCodeBlock, out block):
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

	private Block ReadHeader()
	{
		// skip the first, then remaining # header token
		var start = Position;
		Position++;
		var headerOffset = ConsumeCharacters('#');
		var whitespaceOffset = ConsumeWhitespace();
		ConsumeRestOfLine();
		return CreateOrUpdateSection(MarkdownTokenizer.TokenTypeHeader, start, Position, headerOffset, whitespaceOffset);
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

	#endregion
}