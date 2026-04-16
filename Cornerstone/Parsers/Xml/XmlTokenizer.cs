#region References

using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers.Xml;

[SourceReflection]
public class XmlTokenizer : Tokenizer
{
	#region Fields

	public static readonly IReadOnlyList<string> Extensions = ["xml", "csproj", "axaml", "xaml", "slnx"];

	public static readonly int LexerStateInAttributeValueDoubleQuote;
	public static readonly int LexerStateInAttributeValueSingleQuote;
	public static readonly int LexerStateInCData;
	public static readonly int LexerStateInComment;
	public static readonly int LexerStateInDoctype;
	public static readonly int LexerStateInProcessingInstruction;
	public static readonly int LexerStateInTag;
	public static readonly int LexerStateInTagAfterName;
	public static readonly int LexerStateInText;

	public static readonly int TokenTypeAttributeName;
	public static readonly int TokenTypeAttributeValue;
	public static readonly int TokenTypeCData;
	public static readonly int TokenTypeComment;
	public static readonly int TokenTypeDocType;
	public static readonly int TokenTypeEmptyElementClose;
	public static readonly int TokenTypeEndTagOpen;
	public static readonly int TokenTypeEntityReference;
	public static readonly int TokenTypeEquals;
	public static readonly int TokenTypeProcessingInstruction;
	public static readonly int TokenTypeStartTagOpen;
	public static readonly int TokenTypeTagClose;
	public static readonly int TokenTypeTagName;

	#endregion

	#region Constructors

	public XmlTokenizer(StringGapBuffer buffer, IQueue<Token> pool)
		: base(buffer, pool)
	{
		CurrentState = LexerStateDefault;
	}

	static XmlTokenizer()
	{
		LexerStateInAttributeValueDoubleQuote = RegisterTokenState(nameof(XmlTokenizer), nameof(LexerStateInAttributeValueDoubleQuote), 400);
		LexerStateInAttributeValueSingleQuote = RegisterTokenState(nameof(XmlTokenizer), nameof(LexerStateInAttributeValueSingleQuote), 401);
		LexerStateInCData = RegisterTokenState(nameof(XmlTokenizer), nameof(LexerStateInCData), 402);
		LexerStateInComment = RegisterTokenState(nameof(XmlTokenizer), nameof(LexerStateInComment), 403);
		LexerStateInDoctype = RegisterTokenState(nameof(XmlTokenizer), nameof(LexerStateInDoctype), 404);
		LexerStateInProcessingInstruction = RegisterTokenState(nameof(XmlTokenizer), nameof(LexerStateInProcessingInstruction), 405);
		LexerStateInTag = RegisterTokenState(nameof(XmlTokenizer), nameof(LexerStateInTag), 406);
		LexerStateInText = RegisterTokenState(nameof(XmlTokenizer), nameof(LexerStateInText), 407);
		LexerStateInTagAfterName = RegisterTokenState(nameof(XmlTokenizer), nameof(LexerStateInTagAfterName), 408);

		TokenTypeAttributeName = RegisterTokenType("Attribute Name", nameof(XmlTokenizer), nameof(TokenTypeAttributeName), 400, SyntaxColor.Variable);
		TokenTypeAttributeValue = RegisterTokenType("Attribute Value", nameof(XmlTokenizer), nameof(TokenTypeAttributeValue), 401, SyntaxColor.String);
		TokenTypeCData = RegisterTokenType("CData", nameof(XmlTokenizer), nameof(TokenTypeCData), 402, SyntaxColor.Comment);
		TokenTypeComment = RegisterTokenType("Comment", nameof(XmlTokenizer), nameof(TokenTypeComment), 403, SyntaxColor.Comment);
		TokenTypeDocType = RegisterTokenType("DocType", nameof(XmlTokenizer), nameof(TokenTypeDocType), 404, SyntaxColor.None);
		TokenTypeEmptyElementClose = RegisterTokenType("Empty Element Close", nameof(XmlTokenizer), nameof(TokenTypeEmptyElementClose), 405, SyntaxColor.None);
		TokenTypeEndTagOpen = RegisterTokenType("End Tag Open", nameof(XmlTokenizer), nameof(TokenTypeEndTagOpen), 406, SyntaxColor.Operator);
		TokenTypeEntityReference = RegisterTokenType("Entity Reference", nameof(XmlTokenizer), nameof(TokenTypeEntityReference), 407, SyntaxColor.None);
		TokenTypeEquals = RegisterTokenType("Equals", nameof(XmlTokenizer), nameof(TokenTypeEquals), 408, SyntaxColor.None);
		TokenTypeProcessingInstruction = RegisterTokenType("Processing Instructions", nameof(XmlTokenizer), nameof(TokenTypeProcessingInstruction), 409, SyntaxColor.Keyword);
		TokenTypeStartTagOpen = RegisterTokenType("Start Tag Open", nameof(XmlTokenizer), nameof(TokenTypeStartTagOpen), 410, SyntaxColor.Preprocessor);
		TokenTypeTagClose = RegisterTokenType("Tag Close", nameof(XmlTokenizer), nameof(TokenTypeTagClose), 411, SyntaxColor.Operator);
		TokenTypeTagName = RegisterTokenType("Tag Name", nameof(XmlTokenizer), nameof(TokenTypeTagName), 412, SyntaxColor.Keyword);
	}

	#endregion

	#region Properties

	public override bool SupportsRebuilding => true;

	#endregion

	#region Methods

	public override bool IsStartCharacter()
	{
		return Buffer[Position] is '<' or '>' or '/' or '&' or '"' or '\'' or '-';
	}

	public override bool TryProcessContinuation(out Token token)
	{
		var start = Position;

		switch (CurrentState)
		{
			case var s when s == LexerStateInText:
			{
				token = ReadText();
				return true;
			}
			case var s when (s == LexerStateInTag)
				|| (s == LexerStateInTagAfterName):
			{
				token = ReadInsideTag(start);
				return true;
			}
			case var s when s == LexerStateInAttributeValueDoubleQuote:
			{
				token = ReadAttributeValue(start, '"');
				return true;
			}
			case var s when s == LexerStateInAttributeValueSingleQuote:
			{
				token = ReadAttributeValue(start, '\'');
				return true;
			}
			case var s when s == LexerStateInComment:
			{
				token = ReadComment(start);
				return true;
			}
			case var s when s == LexerStateInCData:
			{
				token = ReadCData(start);
				return true;
			}
			case var s when s == LexerStateInProcessingInstruction:
			{
				token = ReadProcessingInstruction(start);
				return true;
			}
			case var s when s == LexerStateInDoctype:
			{
				token = ReadDoctype(start);
				return true;
			}
		}

		token = null!;
		return false;
	}

	public override bool TryProcessPosition(out Token token)
	{
		var start = Position;
		var c = Buffer[Position];

		// Structural characters
		switch (c)
		{
			case '<':
			{
				Position++;
				if (Position >= Buffer.Count)
				{
					token = CreateOrUpdateSection(TokenTypeError, start, Position);
					return true;
				}

				var next = Buffer[Position];

				if (next == '/')
				{
					Position++;
					CurrentState = LexerStateInTag;
					token = CreateOrUpdateSection(TokenTypeEndTagOpen, start, Position);
					return true;
				}

				if (next == '!')
				{
					Position++;
					if (((Position + 1) < Buffer.Count)
						&& (Buffer[Position] == '-')
						&& (Buffer[Position + 1] == '-'))
					{
						Position += 2;
						CurrentState = LexerStateInComment;
						token = ReadComment(start);
						return true;
					}

					if (((Position + 6) < Buffer.Count)
						&& Buffer.Equals(Position, "[CDATA["))
					{
						Position += 7;
						CurrentState = LexerStateInCData;
						token = ReadCData(start);
						return true;
					}

					CurrentState = LexerStateInDoctype;
					token = ReadDoctype(start);
					return true;
				}

				if (next == '?')
				{
					Position++;
					CurrentState = LexerStateInProcessingInstruction;
					token = ReadProcessingInstruction(start);
					return true;
				}

				// Normal start tag
				CurrentState = LexerStateInTag;
				token = CreateOrUpdateSection(TokenTypeStartTagOpen, start, Position);
				return true;
			}
			case '>':
			{
				Position++;
				CurrentState = LexerStateDefault;
				token = CreateOrUpdateSection(TokenTypeTagClose, start, Position);
				return true;
			}
			case '/':
			{
				if (((Position + 1) < Buffer.Count)
					&& (Buffer[Position + 1] == '>'))
				{
					Position += 2;
					CurrentState = LexerStateDefault;
					token = CreateOrUpdateSection(TokenTypeEmptyElementClose, start, Position);
					return true;
				}
				Position++;
				token = CreateOrUpdateSection(TokenTypeError, start, Position);
				return true;
			}
			case '&':
			{
				token = ReadEntityReference(start);
				return true;
			}
			default:
			{
				CurrentState = LexerStateInText;
				token = ReadText();
				return true;
			}
		}
	}

	protected override Token ReadText()
	{
		// Always consume the first character.
		var start = Position++;

		// Text content (most forgiving mode, until < or &)
		while (Position < Buffer.Count)
		{
			var c = Buffer[Position];
			if (c is '<' or '&')
			{
				break;
			}

			// In strict XML, newlines inside text are allowed
			Position++;
		}

		CurrentState = LexerStateDefault;
		return CreateOrUpdateSection(TokenTypeText, start, Position);
	}

	private static bool IsNameChar(char c)
	{
		return char.IsLetterOrDigit(c) || c is ':' or '_' or '-' || (c > 127);
	}

	private Token ReadAttributeValue(int start, char quoteChar)
	{
		Position++; // consume opening quote

		while (Position < Buffer.Count)
		{
			if (Buffer[Position] == quoteChar)
			{
				Position++; // consume closing quote
				CurrentState = LexerStateInTagAfterName;
				return CreateOrUpdateSection(TokenTypeAttributeValue, start, Position);
			}
			Position++;
		}

		// Incomplete value (continuation will resume in the same state)
		return CreateOrUpdateSection(TokenTypeAttributeValue, start, Position);
	}

	private Token ReadCData(int start)
	{
		while ((Position + 2) < Buffer.Count)
		{
			if ((Buffer[Position] == ']')
				&& (Buffer[Position + 1] == ']')
				&& (Buffer[Position + 2] == '>'))
			{
				Position += 3;
				CurrentState = LexerStateDefault;
				return CreateOrUpdateSection(TokenTypeCData, start, Position);
			}
			Position++;
		}

		CurrentState = LexerStateDefault;
		return CreateOrUpdateSection(TokenTypeCData, start, Position);
	}

	private Token ReadComment(int start)
	{
		while ((Position + 2) < Buffer.Count)
		{
			if ((Buffer[Position] == '-') &&
				(Buffer[Position + 1] == '-') &&
				(Buffer[Position + 2] == '>'))
			{
				Position += 3;
				CurrentState = LexerStateDefault;
				return CreateOrUpdateSection(TokenTypeComment, start, Position);
			}
			Position++;
		}

		CurrentState = LexerStateDefault;
		return CreateOrUpdateSection(TokenTypeComment, start, Position);
	}

	private Token ReadDoctype(int start)
	{
		while (Position < Buffer.Count)
		{
			if (Buffer[Position] == '>')
			{
				Position++;
				CurrentState = LexerStateDefault;
				return CreateOrUpdateSection(TokenTypeDocType, start, Position);
			}
			Position++;
		}

		return CreateOrUpdateSection(TokenTypeDocType, start, Position);
	}

	private Token ReadEntityReference(int start)
	{
		Position++; // skip &

		while (Position < Buffer.Count)
		{
			var c = Buffer[Position];
			if (c == ';')
			{
				Position++;
				return CreateOrUpdateSection(TokenTypeEntityReference, start, Position);
			}
			if (!IsNameChar(c))
			{
				break;
			}
			Position++;
		}

		CurrentState = LexerStateDefault;
		return CreateOrUpdateSection(TokenTypeError, start, Position);
	}

	private Token ReadInsideTag(int start)
	{
		if (Position >= Buffer.Count)
		{
			CurrentState = LexerStateDefault;
			return CreateOrUpdateSection(TokenTypeError, start, Position);
		}

		var c = Buffer[Position];

		// Whitespace inside tags (emitted for accurate positioning/highlighting)
		if (char.IsWhiteSpace(c))
		{
			while ((Position < Buffer.Count) && char.IsWhiteSpace(Buffer[Position]))
			{
				Position++;
			}
			return CreateOrUpdateSection(TokenTypeWhitespace, start, Position);
		}

		// Tag close / empty-element close
		if (c == '>')
		{
			Position++;
			CurrentState = LexerStateDefault;
			return CreateOrUpdateSection(TokenTypeTagClose, start, Position);
		}
		if ((c == '/') && ((Position + 1) < Buffer.Count) && (Buffer[Position + 1] == '>'))
		{
			Position += 2;
			CurrentState = LexerStateDefault;
			return CreateOrUpdateSection(TokenTypeEmptyElementClose, start, Position);
		}

		// Equals
		if (c == '=')
		{
			Position++;
			return CreateOrUpdateSection(TokenTypeEquals, start, Position);
		}

		// Start of quoted attribute value
		if (c is '"' or '\'')
		{
			CurrentState = c == '"'
				? LexerStateInAttributeValueDoubleQuote
				: LexerStateInAttributeValueSingleQuote;
			return ReadAttributeValue(start, c);
		}

		// Name (tag name or attribute name)
		var tokenType = CurrentState == LexerStateInTag
			? TokenTypeTagName
			: TokenTypeAttributeName;

		while (Position < Buffer.Count)
		{
			c = Buffer[Position];
			if (char.IsWhiteSpace(c) || c is '=' or '>' or '/' or '"' or '\'')
			{
				break;
			}
			if (!IsNameChar(c))
			{
				break;
			}
			Position++;
		}

		if (Position == start)
		{
			CurrentState = LexerStateDefault;
			return CreateOrUpdateSection(TokenTypeError, start, Position);
		}

		if (tokenType == TokenTypeTagName)
		{
			CurrentState = LexerStateInTagAfterName;
		}

		return CreateOrUpdateSection(tokenType, start, Position);
	}

	private Token ReadProcessingInstruction(int start)
	{
		while ((Position + 1) < Buffer.Count)
		{
			if ((Buffer[Position] == '?') && (Buffer[Position + 1] == '>'))
			{
				Position += 2;
				CurrentState = LexerStateDefault;
				return CreateOrUpdateSection(TokenTypeProcessingInstruction, start, Position);
			}
			Position++;
		}

		CurrentState = LexerStateDefault;
		return CreateOrUpdateSection(TokenTypeProcessingInstruction, start, Position);
	}

	#endregion
}