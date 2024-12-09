#region References

using Cornerstone.Serialization;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Parsers.Json;

public class JsonParser : Parser<JsonParserSettings>
{
	#region Constructors

	/// <inheritdoc />
	public JsonParser() : base(new JsonParserSettings())
	{
	}

	/// <inheritdoc />
	public JsonParser(JsonParserSettings settings) : base(settings)
	{
	}

	#endregion

	#region Methods

	public static void ParseArray(JsonTokenizer tokenizer, IObjectConsumer consumer, ISerializationSettings settings = null)
	{
		tokenizer.ParseNextUntilNotWhitespaceAndNewLines(); // skip '['

		if (tokenizer.CurrentToken.Type == JsonTokenType.None)
		{
			throw new ParserException($"Unexpected end of stream reached while parsing array in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
		}

		if (tokenizer.CurrentToken.Type == JsonTokenType.SquaredClose)
		{
			tokenizer.ParseNextUntilNotWhitespaceAndNewLines(); // skip ']'
			consumer.CompleteObject();
			return;
		}

		for (;;)
		{
			// parse value
			ParseValue(tokenizer, consumer, settings);
		
			tokenizer.SkipWhitespaceOrNewLines();

			if (tokenizer.CurrentToken.Type == JsonTokenType.Comma)
			{
				tokenizer.ParseNextUntilNotWhitespaceAndNewLines();
			}
			else if (tokenizer.CurrentToken.Type == JsonTokenType.SquaredClose)
			{
				tokenizer.ParseNextUntilNotWhitespaceAndNewLines(); // skip ']'
				consumer.CompleteObject();
				return;
			}
			else if (tokenizer.CurrentToken.Type == JsonTokenType.None)
			{
				throw new ParserException($"Unexpected end of stream reached while parsing array in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
			}
			else
			{
				throw new ParserException($"Unexpected token in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}. Either ']' or ',' was expected.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
			}
		}
	}

	public static void ParseObject(JsonTokenizer tokenizer, IObjectConsumer consumer, ISerializationSettings settings = null)
	{
		if (consumer is not JsonObject jsonObject)
		{
			return;
		}

		tokenizer.ParseNext(); // skip '{'

		if (tokenizer.CurrentToken.Type == JsonTokenType.CurlyClose)
		{
			tokenizer.ParseNext(); // skip '}'
			jsonObject.CompleteObject();
			return;
		}

		for (;;)
		{
			if (tokenizer.CurrentToken.Type
				is JsonTokenType.Whitespace
				or JsonTokenType.NewLine)
			{
				tokenizer.ParseNextUntilNotWhitespaceAndNewLines();
				continue;
			}

			if (tokenizer.CurrentToken.Type == JsonTokenType.CurlyClose)
			{
				tokenizer.ParseNext(); // skip '}'
				jsonObject.CompleteObject();
				return;
			}

			if (tokenizer.CurrentToken.Type == JsonTokenType.None)
			{
				throw new ParserException($"Unexpected end of stream reached while parsing object in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
			}

			if (tokenizer.CurrentToken.Type != JsonTokenType.String)
			{
				throw new ParserException($"Unexpected token in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}. Property name was expected.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
			}

			// expect string literal
			jsonObject.PropertyName(tokenizer.CurrentToken.GetStringValue());

			// expect ':'
			tokenizer.ParseNextUntilNotWhitespaceAndNewLines();
			if (tokenizer.CurrentToken.Type == JsonTokenType.None)
			{
				throw new ParserException($"Unexpected end of stream reached while parsing object in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
			}
			if (tokenizer.CurrentToken.Type != JsonTokenType.Colon)
			{
				throw new ParserException($"Unexpected token in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}. ':' was expected.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
			}

			// parse value
			tokenizer.ParseNextUntilNotWhitespaceAndNewLines(); // skip ':'
			ParseValue(tokenizer, jsonObject, settings);
			tokenizer.SkipWhitespaceOrNewLines();

			if (tokenizer.CurrentToken.Type == JsonTokenType.None)
			{
				throw new ParserException($"Unexpected end of stream reached while parsing object in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
			}

			if (tokenizer.CurrentToken.Type == JsonTokenType.CurlyClose)
			{
				tokenizer.ParseNextUntilNotWhitespaceAndNewLines(); // skip '}'
				jsonObject.CompleteObject();
				return;
			}

			if (tokenizer.CurrentToken.Type != JsonTokenType.Comma)
			{
				throw new ParserException($"Unexpected token in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}. Either '}}' or ',' was expected.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
			}

			tokenizer.ParseNextUntilNotWhitespaceAndNewLines(); // skip ','
		}
	}

	public static void ParseValue(JsonTokenizer tokenizer, IObjectConsumer consumer, ISerializationSettings settings = null)
	{
		if (tokenizer.CurrentToken.Type == JsonTokenType.None)
		{
			throw new ParserException($"Unexpected end of stream reached in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
		}

		if (tokenizer.CurrentToken.Type == JsonTokenType.CurlyOpen)
		{
			ParseObject(tokenizer, consumer.StartObject(typeof(JsonObject)), settings);
			return;
		}
		if (tokenizer.CurrentToken.Type == JsonTokenType.SquaredOpen)
		{
			ParseArray(tokenizer, consumer.StartObject(typeof(JsonArray)), settings);
			return;
		}

		if (tokenizer.CurrentToken.Type == JsonTokenType.String)
		{
			consumer.String(tokenizer.CurrentToken.GetStringValue());
		}
		else if (tokenizer.CurrentToken.Type == JsonTokenType.Boolean)
		{
			consumer.Boolean((bool) tokenizer.CurrentToken.Value);
		}
		else if (tokenizer.CurrentToken.Type == JsonTokenType.NumberInteger)
		{
			consumer.Number((long) tokenizer.CurrentToken.Value);
		}
		else if (tokenizer.CurrentToken.Type == JsonTokenType.NumberUnsignedInteger)
		{
			consumer.Number((ulong) tokenizer.CurrentToken.Value);
		}
		else if (tokenizer.CurrentToken.Type == JsonTokenType.NumberFloat)
		{
			consumer.Number((double) tokenizer.CurrentToken.Value);
		}
		else if (tokenizer.CurrentToken.Type == JsonTokenType.Null)
		{
			consumer.Null();
		}
		else if (tokenizer.CurrentToken.Type
				is JsonTokenType.Whitespace
				or JsonTokenType.NewLine)
		{
			// Nothing to do
		}
		else
		{
			throw new ParserException($"Expected value in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}, but found '{tokenizer.CurrentToken}'.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
		}

		tokenizer.ParseNext(); // skip value literal
	}

	#endregion
}