#region References

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

	public override Token CreateOrUpdateSection(int type, int startOffset, int endOffset, params int[] offsets)
	{
		if (Pool?.TryDequeue(out var token) == true)
		{
			token.Update(type, startOffset, endOffset, GetColor(type), GetBold(type), GetItalic(type), GetStrikethrough(type));
			return token;
		}

		return new Token(type, startOffset, endOffset, GetColor(type), GetBold(type), GetItalic(type), GetStrikethrough(type));
	}

	public virtual SyntaxColor Get(int type)
	{
		return RegisteredTokenTypeColors.TryGetValue(type, out var color)
			? color
			: SyntaxColor.None;
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

	public virtual SyntaxColor GetColor(int type)
	{
		return RegisteredTokenTypeColors.TryGetValue(type, out var color)
			? color
			: SyntaxColor.None;
	}

	public virtual bool GetItalic(int type)
	{
		return false;
	}

	public virtual bool GetStrikethrough(int type)
	{
		return false;
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

	/// <summary>
	/// Helper method to detect and consume a **nested** delimited token using single characters.
	/// Fully supports nesting (e.g. [outer [inner] more] ).
	/// Returns the entire section from the opening startChar to the matching closing endChar.
	/// </summary>
	/// <param name="startChar"> The starting delimiter (e.g. '[') </param>
	/// <param name="endChar"> The ending delimiter (e.g. ']') </param>
	/// <param name="tokenType"> The token type for the entire nested section </param>
	/// <param name="token"> The resulting token (null if not matched or unclosed) </param>
	/// <returns> True if a complete nested delimited token was processed. </returns>
	protected bool TryProcessDelimitedToken(char startChar, char endChar, int tokenType, out Token token)
	{
		if ((Position >= Buffer.Count) || (Buffer[Position] != startChar))
		{
			token = null;
			return false;
		}

		var start = Position;
		var position = start + 1; // skip the opening startChar
		var nestingLevel = 1; // we already saw one opening

		while (position < Buffer.Count)
		{
			var c = Buffer[position];

			if (c == startChar)
			{
				nestingLevel++;
			}
			else if (c == endChar)
			{
				nestingLevel--;
				if (nestingLevel == 0)
				{
					// Found the matching closing delimiter
					position++; // include the closing char
					Position = position;
					CurrentState = LexerStateDefault;
					token = CreateOrUpdateSection(tokenType, start, position);
					return true;
				}
			}

			position++;
		}

		// Unclosed - we reached EOF without finding matching end
		// You can decide the policy: either treat as error, or as text.
		// Here we fail (return false) so the caller can fall back to plain text.
		CurrentState = LexerStateDefault;
		token = null;
		return false;
	}

	/// <summary>
	/// Helper method to detect and consume a delimited token: startPattern + content + endPattern.
	/// Supports multi-character start/end patterns and optional state management.
	/// </summary>
	/// <param name="startPattern"> The starting delimiter (e.g. \", *, ```) </param>
	/// <param name="endPattern"> The ending delimiter. </param>
	/// <param name="tokenType"> The token type to assign to the entire delimited section </param>
	/// <param name="token"> The token if it matched and was processed. </param>
	/// <returns> True if a delimited token was successfully processed. </returns>
	protected bool TryProcessDelimitedToken(string startPattern, string endPattern, int tokenType, out Token token)
	{
		if (!TryMatch(Position, startPattern))
		{
			token = null;
			return false;
		}

		var start = Position;
		var position = start + startPattern.Length;

		while (position < Buffer.Count)
		{
			// Check for end pattern
			if (TryMatch(position, endPattern))
			{
				position += endPattern.Length;
				CurrentState = LexerStateDefault;
				Position = position;

				// Create token for the entire delimited section (including start + content + end)
				token = CreateOrUpdateSection(tokenType, start, position);
				return true;
			}

			position++;
		}

		// If we reach here, we hit EOF or newline without finding the end pattern
		CurrentState = LexerStateDefault;
		token = null;
		return false;
	}

	#endregion
}