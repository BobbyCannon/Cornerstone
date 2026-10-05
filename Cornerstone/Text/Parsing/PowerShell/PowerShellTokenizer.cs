#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Text.Parsing.PowerShell;

/// <summary>
/// Lexical highlighter for PowerShell scripts (.ps1 / .psm1 / .psd1).
/// Covers comments, strings, variables, keywords, operators, parameters, commands, and numbers.
/// </summary>
[SourceReflection]
public class PowerShellTokenizer : Tokenizer
{
	#region Fields

	public static readonly IReadOnlyList<string> Extensions = ["ps1", "psm1", "psd1"];

	public static readonly int TokenTypeCommand;
	public static readonly int TokenTypeCommentBlock;
	public static readonly int TokenTypeCommentInline;
	public static readonly int TokenTypeIdentifier;
	public static readonly int TokenTypeKeyword;
	public static readonly int TokenTypeNumber;
	public static readonly int TokenTypeOperator;
	public static readonly int TokenTypeParameter;
	public static readonly int TokenTypeString;
	public static readonly int TokenTypeVariable;

	private static readonly HashSet<string> _builtinVariables = new(StringComparer.OrdinalIgnoreCase)
	{
		"true", "false", "null", "_", "psitem", "args", "error", "home", "host", "input",
		"matched", "matches", "myinvocation", "nestedpromptlevel", "pid", "profile", "pscommandpath",
		"psculture", "psdebugcontext", "pshome", "psboundparameters", "psscriptroot", "psuiculture",
		"psversiontable", "pwd", "shellid", "stacktrace", "this"
	};

	private static readonly HashSet<string> _keywords = new(StringComparer.OrdinalIgnoreCase)
	{
		"begin", "break", "catch", "class", "continue", "data", "define", "do", "dynamicparam",
		"else", "elseif", "end", "enum", "exit", "filter", "finally", "for", "foreach", "from",
		"function", "hidden", "if", "in", "param", "process", "return", "static", "switch",
		"throw", "trap", "try", "until", "using", "var", "while", "workflow"
	};

	/// <summary>
	/// Operator names without the leading dash (-eq, -like, …).
	/// </summary>
	private static readonly HashSet<string> _operators = new(StringComparer.OrdinalIgnoreCase)
	{
		"eq", "ne", "gt", "ge", "lt", "le",
		"like", "notlike", "match", "notmatch",
		"contains", "notcontains", "in", "notin",
		"replace", "ireplace", "creplace",
		"and", "or", "not", "xor",
		"band", "bor", "bnot", "bxor", "shl", "shr",
		"is", "isnot", "as",
		"split", "join",
		"icontains", "inotcontains", "ilike", "inotlike", "imatch", "inotmatch", "ieq", "ine", "igt", "ige", "ilt", "ile",
		"ccontains", "cnotcontains", "clike", "cnotlike", "cmatch", "cnotmatch", "ceq", "cne", "cgt", "cge", "clt", "cle"
	};

	#endregion

	#region Constructors

	public PowerShellTokenizer(StringGapBuffer buffer, IQueue<Token> pool)
		: base(buffer, pool)
	{
	}

	static PowerShellTokenizer()
	{
		TokenTypeCommentInline = RegisterTokenType("Comment (inline)", nameof(PowerShellTokenizer), nameof(TokenTypeCommentInline), 500, SyntaxKind.Comment);
		TokenTypeCommentBlock = RegisterTokenType("Comment (block)", nameof(PowerShellTokenizer), nameof(TokenTypeCommentBlock), 501, SyntaxKind.Comment);
		TokenTypeKeyword = RegisterTokenType("Keyword", nameof(PowerShellTokenizer), nameof(TokenTypeKeyword), 502, SyntaxKind.Keyword);
		TokenTypeIdentifier = RegisterTokenType("Identifier", nameof(PowerShellTokenizer), nameof(TokenTypeIdentifier), 503, SyntaxKind.None);
		TokenTypeString = RegisterTokenType("String", nameof(PowerShellTokenizer), nameof(TokenTypeString), 504, SyntaxKind.String);
		TokenTypeVariable = RegisterTokenType("Variable", nameof(PowerShellTokenizer), nameof(TokenTypeVariable), 505, SyntaxKind.Variable);
		TokenTypeNumber = RegisterTokenType("Number", nameof(PowerShellTokenizer), nameof(TokenTypeNumber), 506, SyntaxKind.Number);
		TokenTypeOperator = RegisterTokenType("Operator", nameof(PowerShellTokenizer), nameof(TokenTypeOperator), 507, SyntaxKind.Operator);
		TokenTypeParameter = RegisterTokenType("Parameter", nameof(PowerShellTokenizer), nameof(TokenTypeParameter), 508, SyntaxKind.Attribute);
		TokenTypeCommand = RegisterTokenType("Command", nameof(PowerShellTokenizer), nameof(TokenTypeCommand), 509, SyntaxKind.Method);
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
		return c is '#' or '<' or '\'' or '"' or '$' or '@' or '-'
			|| char.IsLetter(c)
			|| (c == '_')
			|| char.IsDigit(c);
	}

	protected override bool TryProcessPosition(out Token token)
	{
		var c = Buffer[Position];

		switch (c)
		{
			case '#':
			{
				return ReadInlineComment(out token);
			}
			case '<':
			{
				if (TryPeekNext('#'))
				{
					return ReadBlockComment(out token);
				}
				break;
			}
			case '\'':
			{
				return ReadSingleQuotedString(out token);
			}
			case '"':
			{
				return ReadDoubleQuotedString(out token);
			}
			case '@':
			{
				if (TryReadHereString(out token))
				{
					return true;
				}
				break;
			}
			case '$':
			{
				return ReadVariable(out token);
			}
			case '-':
			{
				if (TryReadDashToken(out token))
				{
					return true;
				}
				break;
			}
			default:
			{
				if (char.IsLetter(c) || (c == '_'))
				{
					return ReadIdentifierOrKeyword(out token);
				}

				if (char.IsDigit(c))
				{
					return ReadNumber(out token);
				}
				break;
			}
		}

		token = null!;
		return false;
	}

	private bool ReadBlockComment(out Token token)
	{
		var start = Position;
		Position += 2; // <#

		while (Position < Buffer.Count)
		{
			if ((Buffer[Position] == '#') && ((Position + 1) < Buffer.Count) && (Buffer[Position + 1] == '>'))
			{
				Position += 2;
				break;
			}

			Position++;
		}

		token = CreateOrUpdateSection(TokenTypeCommentBlock, start, Position);
		return true;
	}

	private bool ReadDoubleQuotedString(out Token token)
	{
		var start = Position;
		Position++; // opening "

		while (Position < Buffer.Count)
		{
			var c = Buffer[Position];
			if (c == '`')
			{
				Position++;
				if (Position < Buffer.Count)
				{
					Position++;
				}
				continue;
			}

			if (c == '"')
			{
				Position++;
				break;
			}

			Position++;
		}

		token = CreateOrUpdateSection(TokenTypeString, start, Position);
		return true;
	}

	private bool ReadIdentifierOrKeyword(out Token token)
	{
		var start = Position;
		Position++;

		while (Position < Buffer.Count)
		{
			var ch = Buffer[Position];
			if (char.IsLetterOrDigit(ch) || (ch == '_') || (ch == '-'))
			{
				Position++;
			}
			else
			{
				break;
			}
		}

		var word = Buffer.Substring(start, Position - start);
		int type;
		if (_keywords.Contains(word))
		{
			type = TokenTypeKeyword;
		}
		else if (word.Contains('-'))
		{
			type = TokenTypeCommand;
		}
		else
		{
			type = TokenTypeIdentifier;
		}

		token = CreateOrUpdateSection(type, start, Position);
		return true;
	}

	private bool ReadInlineComment(out Token token)
	{
		var start = Position;
		Position++; // #

		while ((Position < Buffer.Count) && Buffer[Position] is not '\r' and not '\n')
		{
			Position++;
		}

		token = CreateOrUpdateSection(TokenTypeCommentInline, start, Position);
		return true;
	}

	private bool ReadNumber(out Token token)
	{
		var start = Position;
		Position++;

		while (Position < Buffer.Count)
		{
			var c = Buffer[Position];

			// Digits, hex/exponent markers, and letter suffixes (kb, mb, L, ...).
			if (char.IsDigit(c) || char.IsLetter(c) || c is '.' or '+' or '-')
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

	private bool ReadSingleQuotedString(out Token token)
	{
		var start = Position;
		Position++; // opening '

		while (Position < Buffer.Count)
		{
			var c = Buffer[Position];
			if (c == '\'')
			{
				Position++;

				// '' is an escaped quote inside single-quoted strings
				if ((Position < Buffer.Count) && (Buffer[Position] == '\''))
				{
					Position++;
					continue;
				}
				break;
			}

			Position++;
		}

		token = CreateOrUpdateSection(TokenTypeString, start, Position);
		return true;
	}

	private bool ReadVariable(out Token token)
	{
		var start = Position;
		Position++; // $

		if (Position >= Buffer.Count)
		{
			token = CreateOrUpdateSection(TokenTypeVariable, start, Position);
			return true;
		}

		if (Buffer[Position] == '{')
		{
			Position++;
			while ((Position < Buffer.Count) && (Buffer[Position] != '}'))
			{
				Position++;
			}

			if (Position < Buffer.Count)
			{
				Position++; // closing }
			}

			token = CreateOrUpdateSection(TokenTypeVariable, start, Position);
			return true;
		}

		// $true / $name / $_ / $1
		if (char.IsLetter(Buffer[Position]) || Buffer[Position] is '_' or '?')
		{
			var nameStart = Position;
			Position++;
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

			var name = Buffer.Substring(nameStart, Position - nameStart);
			var type = _builtinVariables.Contains(name) ? TokenTypeKeyword : TokenTypeVariable;
			token = CreateOrUpdateSection(type, start, Position);
			return true;
		}

		if (char.IsDigit(Buffer[Position]))
		{
			while ((Position < Buffer.Count) && char.IsDigit(Buffer[Position]))
			{
				Position++;
			}

			token = CreateOrUpdateSection(TokenTypeVariable, start, Position);
			return true;
		}

		// Lone $ or unusual form — still mark as variable span of one.
		token = CreateOrUpdateSection(TokenTypeVariable, start, Position);
		return true;
	}

	private bool TryReadDashToken(out Token token)
	{
		if ((Position + 1) >= Buffer.Count)
		{
			token = null!;
			return false;
		}

		var next = Buffer[Position + 1];
		if (!char.IsLetter(next))
		{
			token = null!;
			return false;
		}

		var start = Position;
		Position++; // -

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

		var name = Buffer.Substring(start + 1, Position - start - 1);

		// Single-letter flags (-f, -c, -o) are CLI parameters, not -eq style operators.
		// "-f" is also PowerShell's format operator; treat it as a parameter so
		// "dotnet publish -f net10.0-ios" highlights correctly.
		if ((name.Length > 1) && _operators.Contains(name))
		{
			token = CreateOrUpdateSection(TokenTypeOperator, start, Position);
			return true;
		}

		// Optional ":Name" on parameters: -p:RuntimeIdentifier
		if ((Position < Buffer.Count) && (Buffer[Position] == ':'))
		{
			var afterColon = Position + 1;
			if ((afterColon < Buffer.Count) && (char.IsLetter(Buffer[afterColon]) || (Buffer[afterColon] == '_')))
			{
				Position++;
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
			}
		}

		token = CreateOrUpdateSection(TokenTypeParameter, start, Position);
		return true;
	}

	private bool TryReadHereString(out Token token)
	{
		// @"..."@ or @'...'@ — opening @ must be followed by quote then newline.
		if ((Position + 1) >= Buffer.Count)
		{
			token = null!;
			return false;
		}

		var quote = Buffer[Position + 1];
		if (quote is not '"' and not '\'')
		{
			token = null!;
			return false;
		}

		var start = Position;
		Position += 2;

		// Optional newline after opener
		if ((Position < Buffer.Count) && (Buffer[Position] == '\r'))
		{
			Position++;
		}
		if ((Position < Buffer.Count) && (Buffer[Position] == '\n'))
		{
			Position++;
		}

		while (Position < Buffer.Count)
		{
			if ((Buffer[Position] == quote)
				&& ((Position + 1) < Buffer.Count)
				&& (Buffer[Position + 1] == '@'))
			{
				Position += 2;
				token = CreateOrUpdateSection(TokenTypeString, start, Position);
				return true;
			}

			Position++;
		}

		token = CreateOrUpdateSection(TokenTypeString, start, Position);
		return true;
	}

	#endregion
}