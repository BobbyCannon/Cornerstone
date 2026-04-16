#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers.CSharp;

[SourceReflection]
public class CSharpTokenizer : Tokenizer
{
	#region Fields

	public static readonly IReadOnlyList<string> Extensions = ["csharp", "c#", "cs", "csx"];

	public static readonly int LexerStateInCommentInline;

	public static readonly int TokenTypeAttribute;
	public static readonly int TokenTypeCharLiteral;
	public static readonly int TokenTypeCommentInline;
	public static readonly int TokenTypeCommentMultiline;
	public static readonly int TokenTypeCommentXml;
	public static readonly int TokenTypeIdentifier;
	public static readonly int TokenTypeKeyword;
	public static readonly int TokenTypeNumber;
	public static readonly int TokenTypeString;

	private static readonly HashSet<string> _keywords = new(StringComparer.Ordinal)
	{
		// Full C# keyword list (reserved + contextual where commonly highlighted)
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
		"class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
		"enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
		"foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
		"long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
		"private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
		"short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
		"true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
		"virtual", "void", "volatile", "while",

		// Common contextual keywords (often highlighted as keywords)
		"add", "alias", "ascending", "async", "await", "by", "descending", "dynamic", "equals",
		"from", "get", "global", "group", "into", "join", "let", "nameof", "on", "orderby",
		"partial", "record", "remove", "select", "set", "value", "var", "when", "where", "yield"
	};

	#endregion

	#region Constructors

	public CSharpTokenizer(StringGapBuffer buffer, IQueue<Token> pool)
		: base(buffer, pool)
	{
	}

	static CSharpTokenizer()
	{
		LexerStateInCommentInline = RegisterTokenState(nameof(CSharpTokenizer), nameof(LexerStateInCommentInline), 100);

		TokenTypeAttribute = RegisterTokenType("Attribute", nameof(CSharpTokenizer), nameof(TokenTypeAttribute), 100, SyntaxColor.Attribute);
		TokenTypeCommentInline = RegisterTokenType("Comment (inline)", nameof(CSharpTokenizer), nameof(TokenTypeCommentInline), 101, SyntaxColor.Comment);
		TokenTypeCommentMultiline = RegisterTokenType("Comment (multiline)", nameof(CSharpTokenizer), nameof(TokenTypeCommentMultiline), 102, SyntaxColor.Comment);
		TokenTypeCommentXml = RegisterTokenType("Comment (xml)", nameof(CSharpTokenizer), nameof(TokenTypeCommentXml), 103, SyntaxColor.Comment);
		TokenTypeKeyword = RegisterTokenType("Keyword", nameof(CSharpTokenizer), nameof(TokenTypeKeyword), 104, SyntaxColor.Keyword);
		TokenTypeIdentifier = RegisterTokenType("Identifier", nameof(CSharpTokenizer), nameof(TokenTypeIdentifier), 105, SyntaxColor.None);
		TokenTypeString = RegisterTokenType("String", nameof(CSharpTokenizer), nameof(TokenTypeString), 106, SyntaxColor.String);
		TokenTypeCharLiteral = RegisterTokenType("Character", nameof(CSharpTokenizer), nameof(TokenTypeCharLiteral), 107, SyntaxColor.String);
		TokenTypeNumber = RegisterTokenType("Number", nameof(CSharpTokenizer), nameof(TokenTypeNumber), 108, SyntaxColor.Number);
	}

	#endregion

	#region Properties

	public override bool SupportsRebuilding => true;

	#endregion

	#region Methods

	public override bool IsStartCharacter()
	{
		if (Position >= Buffer.Count)
		{
			return false;
		}
		var c = Buffer[Position];
		return IsStartCharacter(c, AtEndOfLine, AtWhitespace);
	}

	public static bool IsStartCharacter(char c, bool wasEndOfLine, bool wasWhitespace)
	{
		// Conservative triggers only for things that can start a real token
		return c is '/' or '[' or '"' or '\'' or '$' or '@'
			|| char.IsLetter(c)
			|| (c == '_')
			|| char.IsDigit(c);
	}

	public override bool TryProcessPosition(out Token token)
	{
		var c = Buffer[Position];

		switch (c)
		{
			case '/':
			{
				if (AtWhitespace && TryMatch(Position, "///"))
				{
					return ReadCommentOfXmlComment(out token);
				}
				if (TryPeekNext('/'))
				{
					return ReadCommentOfInline(out token);
				}
				if (TryPeekNext('*'))
				{
					return ReadCommentOfMultiline(out token);
				}
				break;
			}
			case '[':
			{
				if (AtWhitespace && TryReadAttribute(out token))
				{
					return true;
				}
				break;
			}
			case '"':
			case '\'':
			{
				return TryReadStringOrChar(out token);
			}
			case '$':
			case '@':
			{
				if (TryReadStringOrChar(out token)) // handles $", @", $@" , """ etc.
				{
					return true;
				}
				break;
			}
			default:
				if (char.IsLetter(c) || (c == '_'))
				{
					return TryReadIdentifierOrKeyword(out token);
				}

				if (char.IsDigit(c))
				{
					return TryReadNumber(out token); // basic for now
				}
				break;
		}

		token = null!;
		return false;
	}

	private bool ReadCommentOfInline(out Token token)
	{
		var start = Position;
		Position += 2;
		while ((Position < Buffer.Count) && Buffer[Position] is not '\r' and not '\n')
		{
			Position++;
		}

		token = CreateOrUpdateSection(TokenTypeCommentInline, start, Position);
		return true;
	}

	private bool ReadCommentOfMultiline(out Token token)
	{
		var start = Position;
		Position += 2;
		while ((Position + 1) < Buffer.Count)
		{
			if ((Buffer[Position] == '*') && (Buffer[Position + 1] == '/'))
			{
				Position += 2;
				token = CreateOrUpdateSection(TokenTypeCommentMultiline, start, Position);
				return true;
			}
			Position++;
		}
		token = CreateOrUpdateSection(TokenTypeCommentMultiline, start, Position);
		return true;
	}

	private bool ReadCommentOfXmlComment(out Token token)
	{
		var start = Position;
		ConsumeRestOfLine();

		while (Position < Buffer.Count)
		{
			var next = CalculatePastWhitespace(Position);
			if (TryMatch(next, "///"))
			{
				Position = next;
				ConsumeRestOfLine();
			}
			else
			{
				break;
			}
		}

		token = CreateOrUpdateSection(TokenTypeCommentXml, start, Position);
		return true;
	}

	private bool TryReadAttribute(out Token token)
	{
		return TryProcessDelimitedToken('[', ']', TokenTypeAttribute, out token);
	}

	private bool TryReadIdentifierOrKeyword(out Token token)
	{
		var start = Position;
		Position++; // first char already known to be letter or _

		while (Position < Buffer.Count)
		{
			var ch = Buffer[Position];
			if (char.IsLetterOrDigit(ch) || (ch == '_'))
			{
				Position++;
			}
			else
			{
				break;
			}
		}

		var length = Position - start;
		var word = Buffer.Substring(start, length);

		var type = _keywords.Contains(word) ? TokenTypeKeyword : TokenTypeIdentifier;

		token = CreateOrUpdateSection(type, start, Position);
		return true;
	}

	private bool TryReadNumber(out Token token)
	{
		var start = Position;
		Position++;

		while (Position < Buffer.Count)
		{
			var c = Buffer[Position];
			if (char.IsDigit(c) || c is '.' or 'e' or 'E' or '+' or '-' or 'f' or 'F' or 'L' or 'l' or 'u' or 'U')
			{
				Position++;
			}
			else
			{
				break;
			}
		}

		token = CreateOrUpdateSection(TokenTypeNumber, start, Position);
		return true;
	}

	private bool TryReadStringOrChar(out Token token)
	{
		var start = Position;
		var c = Buffer[Position];

		// Handle raw string literals (""", """" etc.) - simplified
		if ((c == '"') && TryMatch(Position, "\"\"\""))
		{
			return TryProcessDelimitedToken("\"\"\"", "\"\"\"", TokenTypeString, out token); // basic, doesn't handle variable quote count perfectly
		}

		// Interpolated or verbatim prefixes
		//var isInterpolated = false;
		var isVerbatim = false;

		if (c == '$')
		{
			//isInterpolated = true;
			Position++;
			if ((Position < Buffer.Count) && (Buffer[Position] == '@'))
			{
				isVerbatim = true;
				Position++;
			}
		}
		else if (c == '@')
		{
			isVerbatim = true;
			Position++;
			if ((Position < Buffer.Count) && (Buffer[Position] == '$'))
			{
				//isInterpolated = true;
				Position++;
			}
		}

		// Now expect opening quote
		if ((Position >= Buffer.Count) || (Buffer[Position] != '"'))
		{
			// rollback if we consumed $ or @
			Position = start;
			token = null!;
			return false;
		}

		Position++; // consume opening "

		if (isVerbatim)
		{
			// Verbatim: consume until closing " (no escape processing)
			while (Position < Buffer.Count)
			{
				if ((Buffer[Position] == '"') && TryPeekNext('"'))
				{
					Position += 2; // escaped quote in verbatim
					continue;
				}
				if (Buffer[Position] == '"')
				{
					Position++;
					token = CreateOrUpdateSection(TokenTypeString, start, Position);
					return true;
				}
				Position++;
			}
		}
		else
		{
			// Normal / interpolated string: handle escapes
			while (Position < Buffer.Count)
			{
				if (Buffer[Position] == '\\')
				{
					Position += 2; // skip escape + next char (simplified)
					continue;
				}
				if (Buffer[Position] == '"')
				{
					Position++;
					token = CreateOrUpdateSection(TokenTypeString, start, Position);
					return true;
				}
				Position++;
			}
		}

		// Unclosed string
		token = CreateOrUpdateSection(TokenTypeString, start, Position); // or TokenTypeError
		return true;
	}

	#endregion
}