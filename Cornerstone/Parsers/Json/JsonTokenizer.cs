#region References

using System.Collections.Generic;
using System.Globalization;

#if NETSTANDARD2_0
using Cornerstone.Extensions;
#endif

#endregion

namespace Cornerstone.Parsers.Json;

/// <summary>
/// Represents a tokenizer for a JSON string.
/// </summary>
public sealed class JsonTokenizer
	: Tokenizer<JsonTokenData, JsonTokenType>
{
	#region Fields

	private readonly Stack<bool> _expectingPropertyStack;
	private bool _nextStringIsProperty;

	#endregion

	#region Constructors

	public JsonTokenizer()
	{
		_expectingPropertyStack = new Stack<bool>();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool ParseNext()
	{
		for (;;)
		{
			CurrentToken.IsPropertyName = false;
			CurrentToken.LineNumber = LineNumber;
			CurrentToken.ColumnNumber = ColumnNumber;
			CurrentToken.StartIndex = ReadIndex;
			CurrentToken.Value = null;

			var c = NextChar();

			if (c == (char) 0)
			{
				CurrentToken.EndIndex = ReadIndex;
				CurrentToken.Type = JsonTokenType.None;
				return false;
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
				CurrentToken.EndIndex = ReadIndex;

				if (IsWhitespace(c))
				{
					ParseWhitespace();
					CurrentToken.Type = JsonTokenType.Whitespace;
					CurrentToken.EndIndex = ReadIndex;
					return true;
				}

				if (IsNewLine(c))
				{
					ParseNewLines();
					CurrentToken.Type = JsonTokenType.NewLine;
					CurrentToken.EndIndex = ReadIndex;
					return true;
				}

				if (c == 't')
				{
					Expect("true");
					CurrentToken.Type = JsonTokenType.Boolean;
					CurrentToken.Value = true;
					CurrentToken.EndIndex = ReadIndex;
					return true;
				}
				if (c == 'N')
				{
					Expect("NaN");
					CurrentToken.Type = JsonTokenType.NumberFloat;
					CurrentToken.Value = float.NaN;
					CurrentToken.EndIndex = ReadIndex;
					return true;
				}
				if (c == 'n')
				{
					Expect("null");
					CurrentToken.Type = JsonTokenType.Null;
					CurrentToken.Value = null;
					CurrentToken.EndIndex = ReadIndex;
					return true;
				}
				if (c == 'f')
				{
					Expect("false");
					CurrentToken.Type = JsonTokenType.Boolean;
					CurrentToken.Value = false;
					CurrentToken.EndIndex = ReadIndex;
					return true;
				}
				if (c is '-' or '+' || char.IsDigit(c))
				{
					ParseNumber(c, CurrentToken);
					CurrentToken.EndIndex = ReadIndex;
					return true;
				}
				if (c is '\"' or '\'')
				{
					CurrentToken.Type = JsonTokenType.String;
					ParseString(c);
					CurrentToken.IsPropertyName = _nextStringIsProperty;
					CurrentToken.EndIndex = ReadIndex;
					_nextStringIsProperty = false;
					return true;
				}
				if (c == '{')
				{
					CurrentToken.Type = JsonTokenType.CurlyOpen;
					_expectingPropertyStack.Push(true);
					_nextStringIsProperty = true;
					return true;
				}
				if (c == '}')
				{
					CurrentToken.Type = JsonTokenType.CurlyClose;
					_expectingPropertyStack.TryPop(out _);
					return true;
				}
				if (c == '[')
				{
					CurrentToken.Type = JsonTokenType.SquaredOpen;
					_expectingPropertyStack.Push(false);
					return true;
				}
				if (c == ']')
				{
					CurrentToken.Type = JsonTokenType.SquaredClose;
					_expectingPropertyStack.TryPop(out _);
					return true;
				}
				if (c == ':')
				{
					CurrentToken.Type = JsonTokenType.Colon;
					return true;
				}
				if (c == ',')
				{
					CurrentToken.Type = JsonTokenType.Comma;
					_nextStringIsProperty = _expectingPropertyStack.TryPeek(out var value) && value;
					return true;
				}
				if (c < 32)
				{
					throw new ParserException($"Unexpected control character 0x{(int) c:X2} in line {LineNumber} at position {ColumnNumber}.", LineNumber, ColumnNumber);
				}

				throw new ParserException($"Unexpected character '{c}' in line {LineNumber} at position {ColumnNumber}.", LineNumber, ColumnNumber);
			}
		}
	}

	public void ParseNextUntilNotWhitespaceAndNewLines()
	{
		ParseNext();
		SkipWhitespaceOrNewLines();
	}

	public void SkipWhitespaceOrNewLines()
	{
		while (CurrentToken.Type
				is JsonTokenType.Whitespace
				or JsonTokenType.NewLine)
		{
			ParseNext();
		}
	}

	/// <summary>
	/// Parse a string out of the buffer.
	/// </summary>
	/// <param name="quote"> The quote character. </param>
	/// <returns> The string parsed. </returns>
	protected override void ParseString(char quote)
	{
		var escaped = false;
		for (;;)
		{
			var c = NextChar();
			if (c == (char) 0)
			{
				throw new ParserException($"Unexpected end of stream reached while parsing string in line {LineNumber} at position {ColumnNumber}.", LineNumber, ColumnNumber);
			}

			if (escaped)
			{
				escaped = false;
			}
			else if (c == quote)
			{
				return;
			}
			else if (c == '\\')
			{
				escaped = true;
			}
		}
	}

	private void ParseNumber(char c, JsonTokenData tokenData)
	{
		var dotAppeared = false;
		var exponentAppeared = false;
		var minusAppeared = c == '-';

		for (;;)
		{
			c = PeekChar();

			if (char.IsDigit(c))
			{
				NextChar();
			}
			else if (c == '.')
			{
				NextChar();
				if (dotAppeared)
				{
					throw new ParserException($"The dot '.' must not appear twice in the number literal in line {LineNumber} at position {ColumnNumber}.", LineNumber, ColumnNumber);
				}
				dotAppeared = true;
			}
			else if (c is 'E' or 'e')
			{
				exponentAppeared = true;

				NextChar();
				c = PeekChar();

				if (c is '-' or '+')
				{
					NextChar();
				}
			}
			else
			{
				break;
			}
		}

		CurrentToken.EndIndex = ReadIndex;

		var sValue = CurrentToken.ToString();

		if (dotAppeared || exponentAppeared)
		{
			if (!double.TryParse(sValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
			{
				throw new ParserException($"Invalid number '{ToString()}' in line {LineNumber} at position {ColumnNumber}.", LineNumber, ColumnNumber);
			}

			tokenData.Value = floatValue;
			tokenData.Type = JsonTokenType.NumberFloat;
		}
		else if (minusAppeared)
		{
			if (!long.TryParse(sValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
			{
				throw new ParserException($"Invalid number '{ToString()}' in line {LineNumber} at position {ColumnNumber}.", LineNumber, ColumnNumber);
			}

			tokenData.Value = integerValue;
			tokenData.Type = JsonTokenType.NumberInteger;
		}
		else
		{
			if (!ulong.TryParse(sValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unsignedIntegerValue))
			{
				throw new ParserException($"Invalid number '{ToString()}' in line {LineNumber} at position {ColumnNumber}.", LineNumber, ColumnNumber);
			}

			tokenData.Value = unsignedIntegerValue;
			tokenData.Type = JsonTokenType.NumberUnsignedInteger;
		}
	}

	#endregion
}