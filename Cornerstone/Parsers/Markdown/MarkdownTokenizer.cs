#region References

using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers.Markdown;

[SourceReflection]
public class MarkdownTokenizer : Tokenizer
{
	#region Fields

	public static readonly IReadOnlyList<string> Extensions = ["markdown", "md"];
	public static readonly int TokenTypeBlockQuote;
	public static readonly int TokenTypeBold;
	public static readonly int TokenTypeBoldAndItalic;
	public static readonly int TokenTypeCodeBlock;
	public static readonly int TokenTypeHeader;
	public static readonly int TokenTypeHorizontalRule;
	public static readonly int TokenTypeInlineCode;
	public static readonly int TokenTypeItalic;
	public static readonly int TokenTypeLink;
	public static readonly int TokenTypeStrikethrough;
	public static readonly int TokenTypeTable;
	public static readonly int TokenTypeUnorderedList;

	#endregion

	#region Constructors

	public MarkdownTokenizer(IStringBuffer buffer, IQueue<Token> pool)
		: base(buffer, pool)
	{
		CurrentState = LexerStateDefault;
	}

	static MarkdownTokenizer()
	{
		// If you add types be sure to add them to MarkdownView.axaml.cs
		TokenTypeCodeBlock = RegisterTokenType("Code Block", nameof(MarkdownTokenizer), nameof(TokenTypeCodeBlock), 300, SyntaxKind.None);
		TokenTypeHeader = RegisterTokenType("Header", nameof(MarkdownTokenizer), nameof(TokenTypeHeader), 301, SyntaxKind.Keyword);
		TokenTypeHorizontalRule = RegisterTokenType("Horizontal Rule", nameof(MarkdownTokenizer), nameof(TokenTypeHorizontalRule), 302, SyntaxKind.Operator);
		TokenTypeBoldAndItalic = RegisterTokenType("Bold / Italic", nameof(MarkdownTokenizer), nameof(TokenTypeBoldAndItalic), 303, SyntaxKind.Keyword);
		TokenTypeBold = RegisterTokenType("Bold", nameof(MarkdownTokenizer), nameof(TokenTypeBold), 304, SyntaxKind.Keyword);
		TokenTypeItalic = RegisterTokenType("Italic", nameof(MarkdownTokenizer), nameof(TokenTypeItalic), 305, SyntaxKind.Keyword);
		TokenTypeStrikethrough = RegisterTokenType("Strikethrough", nameof(MarkdownTokenizer), nameof(TokenTypeStrikethrough), 306, SyntaxKind.Keyword);
		TokenTypeTable = RegisterTokenType("Table", nameof(MarkdownTokenizer), nameof(TokenTypeTable), 307, SyntaxKind.None);
		TokenTypeBlockQuote = RegisterTokenType("Block Quote", nameof(MarkdownTokenizer), nameof(TokenTypeBlockQuote), 308, SyntaxKind.None);
		TokenTypeUnorderedList = RegisterTokenType("Unordered List", nameof(MarkdownTokenizer), nameof(TokenTypeUnorderedList), 309, SyntaxKind.None);
		TokenTypeInlineCode = RegisterTokenType("Inline Code", nameof(MarkdownTokenizer), nameof(TokenTypeInlineCode), 310, SyntaxKind.None);
		TokenTypeLink = RegisterTokenType("Link", nameof(MarkdownTokenizer), nameof(TokenTypeLink), 311, SyntaxKind.Method);
	}

	#endregion

	#region Properties

	public override bool SupportsRebuilding => true;

	#endregion

	#region Methods

	public override bool IsStartCharacter()
	{
		return IsStartCharacter(Buffer[Position], AtEndOfLine, AtIndentation, AtWhitespace);
	}

	public static bool IsStartCharacter(char value, bool wasEndOfLine, bool wasIndentation, bool wasWhitespace)
	{
		return (wasEndOfLine && value is '#' or '~' or '`' or '|' or '>' or '*' or '-' or '+')
			|| (wasIndentation && value is '`' or '>' or '*' or '-' or '+')
			|| (wasWhitespace && value is '~' or '`' or '*' or '_' or '[')
			|| (value == '[');
	}

	protected override bool TryProcessContinuation(out Token token)
	{
		token = null;
		return false;
	}

	protected override bool TryProcessPosition(out Token token)
	{
		var c = Buffer[Position];

		switch (c)
		{
			case '#' when AtEndOfLine && TryReadHeader(out token):
			case '*' when AtEndOfLine && TryReadHorizontalRule(out token):
			case '-' when AtEndOfLine && TryReadHorizontalRule(out token):
			case '_' when AtEndOfLine && TryReadHorizontalRule(out token):
			case '-' when AtIndentation && TryReadUnorderedList(out token):
			case '*' when AtIndentation && TryReadUnorderedList(out token):
			case '+' when AtIndentation && TryReadUnorderedList(out token):
			case '>' when AtEndOfLine && TryReadBlockQuote(out token):
			case '*' when AtWhitespace && TryProcessDelimitedSection("***", "***", TokenTypeBoldAndItalic, out token):
			case '_' when AtWhitespace && TryProcessDelimitedSection("___", "___", TokenTypeBoldAndItalic, out token):
			case '*' when AtWhitespace && TryProcessDelimitedSection("**", "**", TokenTypeBold, out token, '\r', '\n', ' '):
			case '_' when AtWhitespace && TryProcessDelimitedSection("__", "__", TokenTypeBold, out token, '\r', '\n', ' '):
			case '*' when AtWhitespace && TryProcessDelimitedSection("*", "*", TokenTypeItalic, out token, '\r', '\n', ' '):
			case '_' when AtWhitespace && TryProcessDelimitedSection("_", "_", TokenTypeItalic, out token, '\r', '\n', ' '):
			case '~' when AtIndentation && TryReadFencedCodeBlock(out token):
			case '`' when AtIndentation && TryReadFencedCodeBlock(out token):
			case '`' when AtWhitespace && TryProcessDelimitedInlineSelection('`', TokenTypeInlineCode, out token):
			case '`' when AtWhitespace && TryProcessDelimitedSection("`", "`", TokenTypeInlineCode, out token):
			case '~' when AtWhitespace && TryProcessDelimitedSection("~~", "~~", TokenTypeStrikethrough, out token):
			case '[' when TryReadLink(out token):
			case '|' when AtEndOfLine && TryReadTable(out token):
			{
				return true;
			}
			default:
			{
				CurrentState = LexerStateDefault;
				token = ReadText();
				return true;
			}
		}
	}

	/// <summary>
	/// Reads <c>[text](destination)</c>. Token spans the full construct (for editor highlighting).
	/// </summary>
	private bool TryReadLink(out Token token)
	{
		if (!MarkdownLink.TryRead(Buffer, Position,
			    out var start, out var end,
			    out _, out _, out _, out _))
		{
			token = null;
			return false;
		}

		token = CreateOrUpdateSection(TokenTypeLink, start, end);
		CurrentState = LexerStateDefault;
		Position = end;
		return true;
	}

	/// <summary>
	/// Reads a fenced code block (``` or ~~~). Supports incomplete fences for streaming:
	/// if no closer is present yet, the token spans to EOF and remains TokenTypeCodeBlock.
	/// </summary>
	private bool TryReadFencedCodeBlock(out Token token)
	{
		if (!MarkdownFence.TryRead(Buffer, Position, out var fence))
		{
			token = null;
			return false;
		}

		token = CreateOrUpdateSection(TokenTypeCodeBlock, fence.StartOffset, fence.EndOffset);
		CurrentState = LexerStateDefault;
		Position = fence.EndOffset;
		return true;
	}

	private bool TryReadBlockQuote(out Token token)
	{
		token = null;
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

		token = CreateOrUpdateSection(TokenTypeBlockQuote, start, end);
		CurrentState = LexerStateDefault;
		Position = end;
		return true;
	}

	private bool TryReadHeader(out Token token)
	{
		// skip the first, then remaining # header token
		var start = Position;
		Position++;
		ConsumeCharacters('#');
		ConsumeRestOfLine();
		CurrentState = LexerStateDefault;
		token = CreateOrUpdateSection(TokenTypeHeader, start, Position);
		return true;
	}

	/// <summary>
	/// A horizontal rule, use three or more asterisks(***),
	/// dashes(---), or underscores(___) on a line by themselves.
	/// </summary>
	private bool TryReadHorizontalRule(out Token token)
	{
		var start = Position;
		var c = Buffer[Position];
		var endOfLine = CalculateUntilEndOfLine(Position);
		var lastCharacter = CalculateUntilNot(start, c);
		var length = endOfLine - start;
		if ((length >= 3) && (endOfLine == lastCharacter))
		{
			Position = endOfLine;
			token = CreateOrUpdateSection(TokenTypeHorizontalRule, start, Position);
			return true;
		}

		token = null;
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
	private bool TryReadTable(out Token token)
	{
		token = null;
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

		token = CreateOrUpdateSection(TokenTypeTable, tableStart, Position);
		return true;
	}

	/// <summary>
	/// Parses unordered list items (-, *, +) followed by whitespace.
	/// Consumes consecutive lines that start with a valid list marker (respecting indentation).
	/// </summary>
	private bool TryReadUnorderedList(out Token token)
	{
		token = null;
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
			blockEnd = CalculateUntilEndOfLine(start);

			// Move to the next line
			start = CalculatePastEndOfLine(blockEnd);
		}

		token = CreateOrUpdateSection(TokenTypeUnorderedList, blockStart, blockEnd);
		Position = blockEnd;
		return true;
	}

	#endregion
}