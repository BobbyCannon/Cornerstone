#region References

using Cornerstone.Serialization;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Parsers.Json;

public class JsonParser : Parser<JsonParserOptions>
{
	#region Constructors

	/// <inheritdoc />
	public JsonParser() : base(new JsonParserOptions())
	{
	}

	/// <inheritdoc />
	public JsonParser(JsonParserOptions options) : base(options)
	{
	}

	#endregion

	#region Methods

	public static void ParseArray(JsonTokenizer tokenizer, IObjectConsumer consumer, ISerializationOptions settings = null)
	{
		tokenizer.MoveNext(); // skip '['

		if (tokenizer.CurrentToken.Type == JsonTokenType.None)
		{
			throw new ParserException($"Unexpected end of stream reached while parsing array in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
		}

		if (tokenizer.CurrentToken.Type == JsonTokenType.SquaredClose)
		{
			tokenizer.MoveNext(); // skip ']'
			consumer.CompleteObject();
			return;
		}

		for (;;)
		{
			// parse value
			ParseValue(tokenizer, consumer, settings);

			if (tokenizer.CurrentToken.Type == JsonTokenType.Comma)
			{
				tokenizer.MoveNext();
			}
			else if (tokenizer.CurrentToken.Type == JsonTokenType.SquaredClose)
			{
				tokenizer.MoveNext(); // skip ']'
				consumer.CompleteObject();
				return;
			}
			else if (tokenizer.CurrentToken.Type == JsonTokenType.None)
			{
				throw new ParserException($"Unexpected end of stream reached while parsing array in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
			}
			else
			{
				throw new ParserException($"Unexpected token in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}. Either ']' or ',' was expected.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
			}
		}
	}

	public static void ParseObject(JsonTokenizer tokenizer, IObjectConsumer consumer, ISerializationOptions settings = null)
	{
		if (consumer is not JsonObject jsonObject)
		{
			return;
		}

		tokenizer.MoveNext(); // skip '{'

		if (tokenizer.CurrentToken.Type == JsonTokenType.CurlyClose)
		{
			tokenizer.MoveNext(); // skip '}'
			jsonObject.CompleteObject();
			return;
		}

		for (;;)
		{
			if (tokenizer.CurrentToken.Type == JsonTokenType.None)
			{
				throw new ParserException($"Unexpected end of stream reached while parsing object in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
			}

			if (tokenizer.CurrentToken.Type != JsonTokenType.String)
			{
				throw new ParserException($"Unexpected token in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}. Property name was expected.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
			}

			// expect string literal
			jsonObject.PropertyName(tokenizer.CurrentToken.StringValue);

			// expect ':'
			tokenizer.MoveNext();
			if (tokenizer.CurrentToken.Type == JsonTokenType.None)
			{
				throw new ParserException($"Unexpected end of stream reached while parsing object in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
			}
			if (tokenizer.CurrentToken.Type != JsonTokenType.Colon)
			{
				throw new ParserException($"Unexpected token in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}. ':' was expected.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
			}

			// parse value
			tokenizer.MoveNext(); // skip ':'
			ParseValue(tokenizer, jsonObject, settings);

			if (tokenizer.CurrentToken.Type == JsonTokenType.None)
			{
				throw new ParserException($"Unexpected end of stream reached while parsing object in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
			}

			if (tokenizer.CurrentToken.Type == JsonTokenType.CurlyClose)
			{
				tokenizer.MoveNext(); // skip '}'
				jsonObject.CompleteObject();
				return;
			}

			if (tokenizer.CurrentToken.Type != JsonTokenType.Comma)
			{
				throw new ParserException($"Unexpected token in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}. Either '}}' or ',' was expected.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
			}

			tokenizer.MoveNext(); // skip ','
		}
	}

	public static void ParseValue(JsonTokenizer tokenizer, IObjectConsumer consumer, ISerializationOptions settings = null)
	{
		if (tokenizer.CurrentToken.Type == JsonTokenType.None)
		{
			throw new ParserException($"Unexpected end of stream reached in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
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
			consumer.String(tokenizer.CurrentToken.StringValue);
		}
		else if (tokenizer.CurrentToken.Type == JsonTokenType.Boolean)
		{
			consumer.Boolean(tokenizer.CurrentToken.BooleanValue);
		}
		else if (tokenizer.CurrentToken.Type == JsonTokenType.NumberInteger)
		{
			consumer.Number(tokenizer.CurrentToken.IntegerValue);
		}
		else if (tokenizer.CurrentToken.Type == JsonTokenType.NumberUnsignedInteger)
		{
			consumer.Number(tokenizer.CurrentToken.UnsignedIntegerValue);
		}
		else if (tokenizer.CurrentToken.Type == JsonTokenType.NumberFloat)
		{
			consumer.Number(tokenizer.CurrentToken.FloatValue);
		}
		else if (tokenizer.CurrentToken.Type == JsonTokenType.Null)
		{
			consumer.Null();
		}
		else
		{
			throw new ParserException($"Expected value in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}, but found '{tokenizer.CurrentToken}'.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
		}

		tokenizer.MoveNext(); // skip value literal
	}

	#endregion
}