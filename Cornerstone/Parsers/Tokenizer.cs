#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Parsers.CSharp;
using Cornerstone.Parsers.Html;
using Cornerstone.Parsers.Json;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers;

/// <summary>
/// Represents a tokenizer for a file.
/// </summary>
/// <typeparam name="T"> The data for a token. </typeparam>
/// <typeparam name="T2"> The type for the token. </typeparam>
public abstract class Tokenizer<T, T2> : Tokenizer
	where T : TokenData<T, T2>, new()
{
	#region Constructors

	/// <summary>
	/// Initialize the tokenizer.
	/// </summary>
	protected Tokenizer()
	{
		CurrentToken = new T();
		CurrentToken.SetDocument(this);
	}

	#endregion

	#region Properties

	public T CurrentToken { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Read all tokens.
	/// </summary>
	public virtual IEnumerable<T> GetTokens()
	{
		while (ParseNext())
		{
			var token = CurrentToken.ShallowClone();
			token.SetDocument(this);
			yield return token;
		}
	}

	/// <inheritdoc />
	public override string ToCodeSyntaxHtml(bool includeHtml = false)
	{
		var writer = new CodeSyntaxHtmlWriter();
		var tokens = GetTokens().ToList();

		if (includeHtml)
		{
			writer.Start();
		}

		foreach (var t in tokens)
		{
			t.WriteTo(writer);
		}

		if (includeHtml)
		{
			writer.Stop();
		}

		return writer.ToString();
	}

	/// <summary>
	/// Expect the next characters should match the provided expected values.
	/// </summary>
	/// <param name="expected"> The expected data. </param>
	protected void Expect(string expected)
	{
		for (var i = 1; i < expected.Length; i++)
		{
			var c = NextChar();

			if ((c == 0) && (StartIndex >= Length))
			{
				throw new ParserException($"Unexpected end of stream reached while '{expected}' was expected in line {LineNumber} at position {ColumnNumber}.", LineNumber, ColumnNumber);
			}

			if (c != expected[i])
			{
				throw new ParserException($"Expected '{expected}' at position {ColumnNumber} in line {LineNumber}, but found '{expected.Substring(0, i) + c}'.", LineNumber, ColumnNumber);
			}
		}
	}

	/// <summary>
	/// Checks to see if the character is whitespace.
	/// </summary>
	protected bool IsNewLine(char c)
	{
		return c is '\r' or '\n';
	}

	/// <summary>
	/// Checks to see if the character is whitespace.
	/// </summary>
	protected bool IsWhitespace(char c)
	{
		return c is ' ' or '\t';
	}

	protected void ParseNewLines()
	{
		ParseUntilNot('\r', '\n');
	}

	/// <summary>
	/// Parse a string out of the buffer.
	/// </summary>
	/// <param name="quote"> The quote character. </param>
	/// <returns> The string parsed. </returns>
	protected abstract void ParseString(char quote);

	protected void ParseUntil(params char[] expected)
	{
		for (;;)
		{
			var c = PeekChar();

			if (expected.Any(x => x == c))
			{
				break;
			}

			NextChar();
		}
	}

	protected void ParseUntilNot(params char[] expected)
	{
		for (;;)
		{
			var c = PeekChar();

			if (expected.Any(x => x == c))
			{
				NextChar();
				continue;
			}

			break;
		}
	}

	protected void ParseWhitespace()
	{
		ParseUntilNot(' ', '\t');
	}

	#endregion
}

public abstract class Tokenizer : TextDocument
{
	#region Methods

	public static Tokenizer GetByName(string language)
	{
		return language?.ToLower().Trim() switch
		{
			"csharp" => new CSharpTokenizer(),
			"json" => new JsonTokenizer(),
			"markdown" => new MarkdownTokenizer(),
			_ => null
		};
	}

	/// <summary>
	/// Parse to the next token.
	/// </summary>
	public abstract bool ParseNext();

	/// <summary>
	/// Parse a number from the character.
	/// </summary>
	/// <param name="character"> The character to process. </param>
	/// <param name="multiplier"> The multiplier to shift the value. </param>
	/// <returns> The number. </returns>
	public static uint ParseSingleNumber(char character, uint multiplier)
	{
		uint response = character switch
		{
			>= '0' and <= '9' => (uint) (character - '0') * multiplier,
			>= 'A' and <= 'F' => (uint) ((character - 'A') + 10) * multiplier,
			>= 'a' and <= 'f' => (uint) ((character - 'a') + 10) * multiplier,
			_ => 0
		};
		return response;
	}

	public static uint ParseUnicode(char c1, char c2, char c3, char c4)
	{
		var p1 = ParseSingleNumber(c1, 0x1000);
		var p2 = ParseSingleNumber(c2, 0x0100);
		var p3 = ParseSingleNumber(c3, 0x0010);
		var p4 = ParseSingleNumber(c4, 0x0001);

		return p1 + p2 + p3 + p4;
	}

	/// <summary>
	/// Convert the content to HTML representation.
	/// </summary>
	/// <returns> The HTML display version of the value. </returns>
	public abstract string ToCodeSyntaxHtml(bool includeHtml = false);

	/// <summary>
	/// Convert the content to HTML representation.
	/// </summary>
	/// <typeparam name="T"> The tokenizer type. </typeparam>
	/// <param name="value"> The value to process into code syntax html. </param>
	/// <param name="includeHtml"> Include html element with sample css. </param>
	/// <returns> The HTML display version of the value. </returns>
	public static string ToCodeSyntaxHtml<T>(string value, bool includeHtml = false)
		where T : Tokenizer, new()
	{
		var tokenizer = new T();
		tokenizer.Add(value);
		return tokenizer.ToCodeSyntaxHtml(includeHtml);
	}

	public static string ToCodeSyntaxHtml(string language, string code)
	{
		var tokenizer = GetByName(language);
		if (tokenizer == null)
		{
			return code;
		}

		tokenizer.Add(code);
		return tokenizer.ToCodeSyntaxHtml(includeHtml: false);
	}

	#endregion
}