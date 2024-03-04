#region References

using System.Globalization;
using System.IO;

#endregion

namespace Cornerstone.Parsers.Json;

/// <summary>
/// Represents a tokenizer for a JSON string.
/// </summary>
public sealed class JsonTokenizer
	: Tokenizer<JsonTokenData, JsonTokenType>
{
	#region Constructors

	/// <summary>
	/// Initializes a tokenizer for JSON data.
	/// </summary>
	/// <param name="reader"> The text reader. </param>
	public JsonTokenizer(TextReader reader) : base(reader)
	{
		MoveNext();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void MoveNext()
	{
		CurrentToken = NextToken;

		for (;;)
		{
			var c = NextChar();

			if (c == (char) 0)
			{
				NextToken.LineNumber = LineNumber;
				NextToken.Position = ColumnIndex;
				NextToken.Type = JsonTokenType.None;
				return;
			}

			if (IsWhitespace(c))
			{
				continue;
			}

			if (c == '/')
			{
				Expect("//");
				while ((c = NextChar()) != (char) 0)
				{
					if (c == '\n') // comment till end of line
					{
						break;
					}
				}
			}
			else
			{
				NextToken.LineNumber = LineNumber;
				NextToken.Position = ColumnIndex;

				if (c == 't')
				{
					Expect("true");
					NextToken.Type = JsonTokenType.Boolean;
					NextToken.BooleanValue = true;
					return;
				}
				if (c == 'N')
				{
					Expect("NaN");
					NextToken.Type = JsonTokenType.NumberFloat;
					NextToken.FloatValue = double.NaN;
					return;
				}
				if (c == 'n')
				{
					Expect("null");
					NextToken.Type = JsonTokenType.Null;
					return;
				}
				if (c == 'f')
				{
					Expect("false");
					NextToken.Type = JsonTokenType.Boolean;
					NextToken.BooleanValue = false;
					return;
				}
				if (c is '-' or '+' || char.IsDigit(c))
				{
					ParseNumber(c, ref NextToken);
					return;
				}
				if (c is '\"' or '\'')
				{
					NextToken.Type = JsonTokenType.String;
					NextToken.StringValue = ParseString(c);
					return;
				}
				if (c == '{')
				{
					NextToken.Type = JsonTokenType.CurlyOpen;
					return;
				}
				if (c == '}')
				{
					NextToken.Type = JsonTokenType.CurlyClose;
					return;
				}
				if (c == '[')
				{
					NextToken.Type = JsonTokenType.SquaredOpen;
					return;
				}
				if (c == ']')
				{
					NextToken.Type = JsonTokenType.SquaredClose;
					return;
				}
				if (c == ':')
				{
					NextToken.Type = JsonTokenType.Colon;
					return;
				}
				if (c == ',')
				{
					NextToken.Type = JsonTokenType.Comma;
					return;
				}
				if (c < 32)
				{
					throw new ParserException($"Unexpected control character 0x{(int) c:X2} in line {LineNumber} at position {ColumnIndex}.", LineNumber, ColumnIndex);
				}

				throw new ParserException($"Unexpected character '{c}' in line {LineNumber} at position {ColumnIndex}.", LineNumber, ColumnIndex);
			}
		}
	}

	/// <summary>
	/// Parse a string out of the buffer.
	/// </summary>
	/// <param name="quote"> The quote character. </param>
	/// <returns> The string parsed. </returns>
	protected override string ParseString(char quote)
	{
		TemporaryBuffer.Clear();
		var escaped = false;
		for (;;)
		{
			var c = NextChar();
			if (c == (char) 0)
			{
				throw new ParserException($"Unexpected end of stream reached while parsing string in line {LineNumber} at position {ColumnIndex}.", LineNumber, ColumnIndex);
			}

			if (escaped)
			{
				switch (c)
				{
					case '0':
					{
						TemporaryBuffer.Append('0');
						break;
					}
					case '\'':
					{
						TemporaryBuffer.Append('\'');
						break;
					}
					case '"':
					{
						TemporaryBuffer.Append('"');
						break;
					}
					case '\\':
					{
						TemporaryBuffer.Append('\\');
						break;
					}
					case '/':
					{
						TemporaryBuffer.Append('/');
						break;
					}
					case 'b':
					{
						TemporaryBuffer.Append('\b');
						break;
					}
					case 'f':
					{
						TemporaryBuffer.Append('\f');
						break;
					}
					case 'n':
					{
						TemporaryBuffer.Append('\n');
						break;
					}
					case 'r':
					{
						TemporaryBuffer.Append('\r');
						break;
					}
					case 't':
					{
						TemporaryBuffer.Append('\t');
						break;
					}
					case 'u':
					{
						var hex1 = NextChar();
						var hex2 = NextChar();
						var hex3 = NextChar();
						var hex4 = NextChar();
						if ((hex1 == 0) || (hex2 == 0) || (hex3 == 0) || (hex4 == 0))
						{
							throw new ParserException($"Unexpected end of stream reached while parsing unicode escape sequence in line {LineNumber} at position {ColumnIndex}.", LineNumber, ColumnIndex);
						}

						// parse the 32 bit hex into an integer codepoint
						var codePoint = ParseUnicode(hex1, hex2, hex3, hex4);
						TemporaryBuffer.Append((char) codePoint);
						break;
					}
					default:
					{
						throw new ParserException($"Invalid character '{c}' in escape sequence in line {LineNumber} at position {ColumnIndex}.", LineNumber, ColumnIndex);
					}
				}
				escaped = false;
			}
			else if (c == quote)
			{
				return TemporaryBuffer.ToString();
			}
			else if (c == '\\')
			{
				escaped = true;
			}
			else
			{
				if (c < 32)
				{
					//throw new ParserException($"Unexpected control character 0x{(int) c:X2} in string literal in line {_lineNo} at position {_columnIndex}.", _lineNo, _columnIndex);
					//JsonString.Escape(c, _stringBuilder);
					TemporaryBuffer.Append(c);
				}
				else
				{
					TemporaryBuffer.Append(c);
				}
			}
		}
	}

	private void ParseNumber(char c, ref JsonTokenData tokenData)
	{
		TemporaryBuffer.Clear();
		TemporaryBuffer.Append(c);

		var dotAppeared = false;
		var exponentAppeared = false;
		var minusAppeared = c == '-';

		for (;;)
		{
			c = PeekChar();

			if (char.IsDigit(c))
			{
				TemporaryBuffer.Append(NextChar());
			}
			else if (c == '.')
			{
				TemporaryBuffer.Append(NextChar());
				if (dotAppeared)
				{
					throw new ParserException($"The dot '.' must not appear twice in the number literal {TemporaryBuffer} in line {LineNumber} at position {ColumnIndex}.", LineNumber, ColumnIndex);
				}
				dotAppeared = true;
			}
			else if ((c == 'E') || (c == 'e'))
			{
				exponentAppeared = true;
				TemporaryBuffer.Append(NextChar());
				c = PeekChar();
				if ((c == '-') || (c == '+'))
				{
					TemporaryBuffer.Append(NextChar());
				}
			}
			else
			{
				break;
			}
		}
		if (dotAppeared || exponentAppeared)
		{
			if (!double.TryParse(TemporaryBuffer.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
			{
				throw new ParserException($"Invalid number '{TemporaryBuffer}' in line {LineNumber} at position {ColumnIndex}.", LineNumber, ColumnIndex);
			}

			tokenData.FloatValue = floatValue;
			tokenData.Type = JsonTokenType.NumberFloat;
		}
		else if (minusAppeared)
		{
			if (!long.TryParse(TemporaryBuffer.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
			{
				throw new ParserException($"Invalid number '{TemporaryBuffer}' in line {LineNumber} at position {ColumnIndex}.", LineNumber, ColumnIndex);
			}

			tokenData.IntegerValue = integerValue;
			tokenData.Type = JsonTokenType.NumberInteger;
		}
		else
		{
			if (!ulong.TryParse(TemporaryBuffer.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var unsignedIntegerValue))
			{
				throw new ParserException($"Invalid number '{TemporaryBuffer}' in line {LineNumber} at position {ColumnIndex}.", LineNumber, ColumnIndex);
			}

			tokenData.UnsignedIntegerValue = unsignedIntegerValue;
			tokenData.Type = JsonTokenType.NumberUnsignedInteger;
		}
	}

	private uint ParseUnicode(char c1, char c2, char c3, char c4)
	{
		var p1 = ParseSingleNumber(c1, 0x1000);
		var p2 = ParseSingleNumber(c2, 0x0100);
		var p3 = ParseSingleNumber(c3, 0x0010);
		var p4 = ParseSingleNumber(c4, 0x0001);

		return p1 + p2 + p3 + p4;
	}

	#endregion
}