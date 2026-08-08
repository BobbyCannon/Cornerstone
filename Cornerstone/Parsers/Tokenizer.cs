#region References

using System.Drawing;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Parsers.CSharp;
using Cornerstone.Parsers.Json;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Parsers.Xml;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers;

/// <summary>
/// Provides functionality for tokenizing text buffers using customizable token states and types. Designed to support
/// extensible lexical analysis and token classification for syntax highlighting or parsing scenarios.
/// </summary>
/// <remarks>
/// Base:       0-99
/// CSharp:   100-199
/// Json:     200-299
/// Markdown: 300-399
/// Xml:      400-499
/// </remarks>
[SourceReflection]
public class Tokenizer : TextProcessor<Token>
{
	#region Fields

	public static readonly int LexerStateInNumber;
	public static readonly int LexerStateInString;

	#endregion

	#region Constructors

	public Tokenizer(IStringBuffer buffer, IQueue<Token> pool) : base(buffer, pool)
	{
		CurrentState = LexerStateDefault;
	}

	static Tokenizer()
	{
		LexerStateInNumber = RegisterTokenState(nameof(Tokenizer), nameof(LexerStateInNumber), 1);
		LexerStateInString = RegisterTokenState(nameof(Tokenizer), nameof(LexerStateInString), 2);
	}

	#endregion

	#region Properties

	public virtual bool SupportsRebuilding => false;

	#endregion

	#region Methods

	public override Token CreateOrUpdateSection(int type, int startOffset, int endOffset, uint? foreground = null, uint? background = null,
		bool? bold = null, bool? italic = null, bool? strikethrough = null, params int[] offsets)
	{
		if (Pool?.TryDequeue(out var token) != true)
		{
			token= new Token();
		}

		token.Update(type, startOffset, endOffset, GetSyntaxKind(type), bold ?? GetBold(type),
			italic ?? GetItalic(type), strikethrough ?? GetStrikethrough(type), foreground, background);

		return token;
	}

	public virtual SyntaxKind Get(int type)
	{
		return RegisteredTokenTypeColors.TryGetValue(type, out var color)
			? color
			: SyntaxKind.None;
	}

	public virtual bool GetBold(int type)
	{
		return false;
	}

	public static Tokenizer GetByExtension(string extension, StringGapBuffer buffer, IQueue<Token> pool)
	{
		var value = extension?.ToLower();
		if (value == null)
		{
			return null;
		}
		if (CSharpTokenizer.Extensions.Contains(value))
		{
			return new CSharpTokenizer(buffer, pool);
		}
		if (JsonTokenizer.Extensions.Contains(value))
		{
			return new JsonTokenizer(buffer, pool);
		}
		if (MarkdownTokenizer.Extensions.Contains(value))
		{
			return new MarkdownTokenizer(buffer, pool);
		}
		if (XmlTokenizer.Extensions.Contains(value))
		{
			return new XmlTokenizer(buffer, pool);
		}
		return null;
	}

	public virtual bool GetItalic(int type)
	{
		return false;
	}

	public virtual bool GetStrikethrough(int type)
	{
		return false;
	}

	public virtual SyntaxKind GetSyntaxKind(int type)
	{
		return RegisteredTokenTypeColors.TryGetValue(type, out var color)
			? color
			: SyntaxKind.None;
	}

	public override bool IsStartCharacter()
	{
		return false;
	}

	public override void StartProcessing()
	{
		CurrentState = LexerStateDefault;
		base.StartProcessing();
	}

	public bool TryMatch(int start, string expected, int type, out Token token)
	{
		if (base.TryMatch(start, expected))
		{
			Position += expected.Length;
			token = CreateOrUpdateSection(type, start, Position);
			return true;
		}

		token = null!;
		return false;
	}

	#endregion
}