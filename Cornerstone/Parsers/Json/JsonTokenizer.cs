#region References

using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers.Json;

[SourceReflection]
public class JsonTokenizer : Tokenizer
{
	#region Fields

	public static readonly IReadOnlyList<string> Extensions = ["json"];

	public static readonly int LexerStateInStringEscape;

	public static readonly int TokenTypeColon;
	public static readonly int TokenTypeComma;
	public static readonly int TokenTypeFalse;
	public static readonly int TokenTypeLeftBrace;
	public static readonly int TokenTypeLeftBracket;
	public static readonly int TokenTypeNull;
	public static readonly int TokenTypeNumber;
	public static readonly int TokenTypeRightBrace;
	public static readonly int TokenTypeRightBracket;
	public static readonly int TokenTypeString;
	public static readonly int TokenTypeTrue;

	#endregion

	#region Constructors

	public JsonTokenizer(StringGapBuffer buffer, IQueue<Token> pool)
		: base(buffer, pool)
	{
	}

	static JsonTokenizer()
	{
		LexerStateInStringEscape = RegisterTokenState(nameof(JsonTokenizer), nameof(LexerStateInStringEscape), 200);

		TokenTypeColon = RegisterTokenType("Colon", nameof(JsonTokenizer), nameof(TokenTypeColon), 200, SyntaxColor.None);
		TokenTypeComma = RegisterTokenType("Comma", nameof(JsonTokenizer), nameof(TokenTypeComma), 201, SyntaxColor.None);
		TokenTypeFalse = RegisterTokenType("False", nameof(JsonTokenizer), nameof(TokenTypeFalse), 202, SyntaxColor.Keyword);
		TokenTypeLeftBrace = RegisterTokenType("Left Brace", nameof(JsonTokenizer), nameof(TokenTypeLeftBrace), 203, SyntaxColor.None);
		TokenTypeLeftBracket = RegisterTokenType("Left Bracket", nameof(JsonTokenizer), nameof(TokenTypeLeftBracket), 204, SyntaxColor.None);
		TokenTypeNull = RegisterTokenType("Null", nameof(JsonTokenizer), nameof(TokenTypeNull), 205, SyntaxColor.Keyword);
		TokenTypeNumber = RegisterTokenType("Number", nameof(JsonTokenizer), nameof(TokenTypeNumber), 206, SyntaxColor.Number);
		TokenTypeRightBrace = RegisterTokenType("Right Brace", nameof(JsonTokenizer), nameof(TokenTypeRightBrace), 207, SyntaxColor.None);
		TokenTypeRightBracket = RegisterTokenType("Right Bracket", nameof(JsonTokenizer), nameof(TokenTypeRightBracket), 208, SyntaxColor.None);
		TokenTypeString = RegisterTokenType("String", nameof(JsonTokenizer), nameof(TokenTypeString), 209, SyntaxColor.String);
		TokenTypeTrue = RegisterTokenType("True", nameof(JsonTokenizer), nameof(TokenTypeTrue), 210, SyntaxColor.Keyword);
	}

	#endregion

	#region Properties

	public override bool SupportsRebuilding => true;

	#endregion

	#region Methods

	public override bool IsStartCharacter()
	{
		return Buffer[Position]
			is ',' or '\r' or '\n' or ' ' or '\t'
			or '{' or '}' or '[' or ']' or ':';
	}

	public override bool TryProcessContinuation(out Token token)
	{
		if (CurrentState == LexerStateInString)
		{
			token = ReadString(Position);
			return true;
		}

		if (CurrentState == LexerStateInStringEscape)
		{
			token = ReadStringEscapeContinuation(Position);
			return true;
		}

		if (CurrentState == LexerStateInNumber)
		{
			token = ReadNumber(Position);
			return true;
		}

		token = null!;
		return false;
	}

	public override bool TryProcessPosition(out Token token)
	{
		var start = Position;
		var c = Buffer[Position];

		// Single-character structural tokens
		switch (c)
		{
			case '{':
			{
				token = CreateOrUpdateSection(TokenTypeLeftBrace, start, ++Position);
				return true;
			}
			case '}':
			{
				token = CreateOrUpdateSection(TokenTypeRightBrace, start, ++Position);
				return true;
			}
			case '[':
			{
				token = CreateOrUpdateSection(TokenTypeLeftBracket, start, ++Position);
				return true;
			}
			case ']':
			{
				token = CreateOrUpdateSection(TokenTypeRightBracket, start, ++Position);
				return true;
			}
			case ':':
			{
				token = CreateOrUpdateSection(TokenTypeColon, start, ++Position);
				return true;
			}
			case ',':
			{
				token = CreateOrUpdateSection(TokenTypeComma, start, ++Position);
				return true;
			}
			case '"':
			{
				Position++;
				CurrentState = LexerStateInString;
				token = ReadString(start);
				return true;
			}
		}

		// Keywords
		if ((c == 't') && TryMatch(start, "true", TokenTypeTrue, out token))
		{
			return true;
		}

		if ((c == 'f') && TryMatch(start, "false", TokenTypeFalse, out token))
		{
			return true;
		}

		if ((c == 'n') && TryMatch(start, "null", TokenTypeNull, out token))
		{
			return true;
		}

		// Numbers
		if ((c == '-') || char.IsDigit(c))
		{
			CurrentState = LexerStateInNumber;
			token = ReadNumber(start);
			return true;
		}

		// Error handling
		while (Position < Buffer.Count)
		{
			if (IsStartCharacter())
			{
				break;
			}
			AtEndOfLine = Newlines.Contains(Buffer[Position]);
			AtWhitespace = Whitespace.Contains(Buffer[Position]);
			Position++;
		}

		token = CreateOrUpdateSection(TokenTypeError, start, Position);
		return true;
	}

	private Token ReadNumber(int start)
	{
		while (Position < Buffer.Count)
		{
			var c = Buffer[Position];
			if (char.IsDigit(c) || c is '.' or 'e' or 'E' or '+' or '-')
			{
				Position++;
				continue;
			}
			break;
		}

		CurrentState = LexerStateDefault;
		return CreateOrUpdateSection(TokenTypeNumber, start, Position);
	}

	private Token ReadString(int start)
	{
		while (Position < Buffer.Count)
		{
			var c = Buffer[Position];

			if (c == '\\')
			{
				Position++;
				if (Position >= Buffer.Count)
				{
					CurrentState = LexerStateInStringEscape;
					return CreateOrUpdateSection(TokenTypeString, start, Position);
				}
				Position++; // skip escaped char
				continue;
			}

			if (c == '"')
			{
				Position++;
				CurrentState = LexerStateDefault;
				return CreateOrUpdateSection(TokenTypeString, start, Position);
			}

			// Treat newline as end of bad string
			if (c is '\r' or '\n')
			{
				CurrentState = LexerStateDefault;
				return CreateOrUpdateSection(TokenTypeError, start, Position);
			}

			Position++;
		}

		CurrentState = LexerStateInString;
		return CreateOrUpdateSection(TokenTypeString, start, Position);
	}

	private Token ReadStringEscapeContinuation(int start)
	{
		if (Position >= Buffer.Count)
		{
			CurrentState = LexerStateInString;
			return CreateOrUpdateSection(TokenTypeString, start, Position);
		}

		Position++; // consume the escaped character
		CurrentState = LexerStateInString;
		return ReadString(start);
	}

	#endregion
}