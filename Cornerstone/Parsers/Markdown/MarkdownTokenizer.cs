#region References

using System.Linq;
using Cornerstone.Parsers.Html;

#endregion

namespace Cornerstone.Parsers.Markdown;

public class MarkdownTokenizer
	: Tokenizer<MarkdownTokenData, MarkdownTokenType>
{
	#region Methods

	/// <inheritdoc />
	public override bool ParseNext()
	{
		for (;;)
		{
			CurrentToken.LineNumber = LineNumber;
			CurrentToken.ColumnNumber = ColumnNumber;
			CurrentToken.StartIndex = ReadIndex;
			CurrentToken.ElementName = null;
			CurrentToken.Value = null;

			var c = NextChar();

			try
			{
				switch (c)
				{
					case (char) 0:
					{
						CurrentToken.Type = MarkdownTokenType.None;
						return false;
					}
					case '>':
					{
						CurrentToken.Type = MarkdownTokenType.BlockQuote;
						CurrentToken.ElementName = "blockquote";
						CurrentToken.TokenIndexes = [CurrentToken.StartIndex + 1];
						ParseUntil('\0', '\r', '\n');
						return true;
					}
					case '#':
					{
						var headerNumber = MatchCharacter(CurrentToken.StartIndex, '#', 6);
						
						// todo: this is broken, it's expect exact 1 character per match
						// meaning ## { } aoeu won't match correctly
						var offsets = FindCharactersIndexes(CurrentToken.StartIndex, expected: ['#', '{', '}'], ignore: [' '], until: ['\r', '\n', '\0']);
						if (offsets.Length > 1)
						{
							CurrentToken.Type = MarkdownTokenType.Header;
							CurrentToken.ElementName = "h" + headerNumber;
							CurrentToken.TokenIndexes = offsets;
							CurrentToken.TokenIndexes[0] += headerNumber;
							MoveTo(offsets[offsets.Length - 1] + 1);
							return true;
						}
						break;
					}
					case '-':
					case '*':
					{
						var peekNext = PeekChar();
						if (peekNext == ' ')
						{
							CurrentToken.Type = MarkdownTokenType.UnorderedList;
							CurrentToken.TokenIndexes = [CurrentToken.StartIndex + MatchCharacter(CurrentToken.StartIndex + 1, ' ') + 1];
							CurrentToken.ElementName = "li";
							ParseUntil('\0', '\r', '\n');
							return true;
						}

						var count = MatchCharacter(CurrentToken.StartIndex, c, 6);
						var keys = new string(c, count);
						var indexes = MatchStrings(CurrentToken.StartIndex, [keys, keys], ['\0']);
						if (indexes.Length > 0)
						{
							CurrentToken.Type = count == 1 ? MarkdownTokenType.Italic : MarkdownTokenType.Bold;
							CurrentToken.TokenIndexes = [indexes[0] + count, indexes[1]];
							CurrentToken.ElementName = count == 1 ? "em" : "strong";
							MoveTo(indexes[1] + count);
							//ParseUntil('\0', '\r', '\n');
							return true;
						}
						break;
					}
					case '1':
					{
						var peekNext = PeekChar();
						if (peekNext == '.')
						{
							CurrentToken.Type = MarkdownTokenType.OrderedList;
							CurrentToken.ElementName = "li";
							ParseUntil('\0', '\r', '\n');
							return true;
						}

						ParseUntil('\0', '\r', '\n');
						CurrentToken.Type = MarkdownTokenType.Text;
						return true;
					}
					case '_':
					{
						var count = MatchCharacter(CurrentToken.StartIndex, '_', 6);
						var keys = new string('_', count);
						var indexes = MatchStrings(CurrentToken.StartIndex, [keys, keys], ['\0']);
						CurrentToken.Type = count == 1 ? MarkdownTokenType.Italic : MarkdownTokenType.Bold;
						CurrentToken.TokenIndexes = [indexes[0] + count, indexes[1]];
						CurrentToken.ElementName = count == 1 ? "em" : "strong";
						ParseUntil('\0', '\r', '\n');
						return true;
					}
					case '[':
					{
						// Try to match a link
						var offsets = FindCharactersPattern(CurrentToken.StartIndex, ['[', ']', '(', ')'], ['\r', '\n', '\0']);
						if (offsets.Length == 4)
						{
							CurrentToken.Type = MarkdownTokenType.Link;
							CurrentToken.ElementName = "a";
							CurrentToken.TokenIndexes = offsets;
							MoveTo(offsets[3] + 1);
							return true;
						}
						break;
					}
					case '`':
					{
						// Try to match a code section
						var offsets = MatchStrings(CurrentToken.StartIndex, ["```", "```"], ['\0']);
						if (offsets.Length == 2)
						{
							CurrentToken.Type = MarkdownTokenType.Code;
							CurrentToken.ElementName = "pre";

							// Get the code block
							if (FindAnyCharacter(offsets[0], offsets[1], ['\n'], out var codeStart)
								&& FindAnyCharacterExceptInReverse(offsets[0], offsets[1], ['`', '\r', '\n'], out var codeEnd)
								&& (codeStart != codeEnd))
							{
								// Check for language
								CurrentToken.TokenIndexes = FindAnyCharacterExcept(offsets[0], codeStart, ['`', '\r', '\n'], out var languageStart)
									&& FindAnyCharacterExceptInReverse(offsets[0], codeStart, ['\r', '\n'], out var languageEnd)
										? [offsets[0], languageStart, languageEnd, codeStart + 1, codeEnd, offsets[1]]
										: [offsets[0], codeStart + 1, codeEnd, offsets[1]];
							}
							else
							{
								CurrentToken.TokenIndexes = offsets;
							}

							MoveTo(offsets[1] + 3);
							return true;
						}
						break;
					}
				}

				if (IsNewLine(c))
				{
					ParseNewLines();
					CurrentToken.Type = MarkdownTokenType.NewLine;
					return true;
				}

				if (IsWhitespace(c))
				{
					ParseWhitespace();
					CurrentToken.Type = MarkdownTokenType.Whitespace;
					return true;
				}

				ParseUntil('*', '\0', '\r', '\n');
				CurrentToken.Type = MarkdownTokenType.Text;
				return true;
			}
			finally
			{
				CurrentToken.EndIndex = ReadIndex;
			}
		}
	}

	public string ToHtml()
	{
		var writer = new HtmlWriter();
		var tokens = GetTokens().ToList();

		foreach (var token in tokens)
		{
			token.WriteTo(writer);
		}

		return writer.ToString();
	}

	public static string ToHtml(string markdown)
	{
		var tokenizer = new MarkdownTokenizer();
		tokenizer.Add(markdown);
		var response = tokenizer.ToHtml();
		return response;
	}

	/// <inheritdoc />
	protected override void ParseString(char quote)
	{
		for (;;)
		{
			var c = NextChar();
			if (c is (char) 0 or '\n' or '\r')
			{
				break;
			}
		}
	}

	#endregion
}