#region References

using System;
using System.Collections.Generic;
using System.IO;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Json;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Consumers;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Storage;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization.Json;

/// <inheritdoc />
public class JsonSerializer : IJsonSerializer
{
	#region Fields

	private static readonly MemoryCache<Type, IJsonConverter> _cache;
	private readonly Func<ISerializationOptions, IObjectConsumer> _consumerProvider;
	private static readonly IReadOnlyList<IJsonConverter> _converters;
	private static readonly IList<IJsonConverter> _customConverters;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public JsonSerializer() : this(x => new TextJsonConsumer(x))
	{
	}

	/// <summary>
	/// Initialize the JSON serializer.
	/// </summary>
	public JsonSerializer(TextWriterStringBuffer buffer)
		: this(x => new TextJsonConsumer(buffer, x))
	{
	}

	/// <summary>
	/// Initialize the JSON serializer.
	/// </summary>
	/// <param name="consumerProvider"> The consumer to use when serializing. </param>
	public JsonSerializer(Func<ISerializationOptions, IObjectConsumer> consumerProvider)
	{
		_consumerProvider = consumerProvider;
	}

	static JsonSerializer()
	{
		_cache = new MemoryCache<Type, IJsonConverter>();
		_customConverters = new List<IJsonConverter>();
		_converters = new List<IJsonConverter>
			{
				new BooleanJsonConverter(),
				new NumberJsonConverter(),
				new EnumJsonConverter(),
				new StringJsonConverter(),
				new DateJsonConverter(),
				new DictionaryConverter(),
				new TimeJsonConverter(),
				new GuidJsonConverter(),
				new VersionJsonConverter(),
				new ArrayJsonConverter(),
				new TupleConverter(),
				new PartialUpdateJsonConverter(),
				new PartialUpdateValueJsonConverter(),
				// This one has to be last
				new ObjectJsonConverter()
			}
			.AsReadOnly();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Convert a JSON value to an object of the provided type.
	/// </summary>
	/// <param name="type"> The type to convert to. </param>
	/// <param name="jsonValue"> The value to convert. </param>
	/// <returns> The instance of the object. </returns>
	public static object Convert(Type type, JsonValue jsonValue)
	{
		var converter = GetConverter(type);
		return converter.ConvertTo(type, jsonValue);
	}

	/// <inheritdoc />
	public T FromJson<T>(string value, ISerializationOptions settings = null)
	{
		var result = Parse(value, settings);
		var type = typeof(T);
		var converter = GetConverter(type);
		return (T) converter.ConvertTo(type, result);
	}

	/// <inheritdoc />
	public object FromJson(string value, Type type, ISerializationOptions settings = null)
	{
		var result = Parse(value, settings);
		var converter = GetConverter(type);
		return converter.ConvertTo(type, result);
	}

	/// <summary>
	/// Get a converter from the registered converters.
	/// </summary>
	/// <param name="type"> The type to find a converter for. </param>
	/// <returns> The converter if one was found. </returns>
	/// <exception cref="NotSupportedException"> The type [{type.FullName}] is not supported. </exception>
	public static IJsonConverter GetConverter(Type type)
	{
		if (_cache.TryGet(type, out var cacheItem))
		{
			return cacheItem.Value;
		}

		foreach (var converter in _customConverters)
		{
			if (converter.CanConvert(type))
			{
				_cache.Set(type, converter);
				return converter;
			}
		}

		foreach (var converter in _converters)
		{
			if (converter.CanConvert(type))
			{
				_cache.Set(type, converter);
				return converter;
			}
		}

		throw new NotSupportedException($"The type [{type.FullName}] is not supported.");
	}

	/// <summary>
	/// Parse the value into JsonValues.
	/// </summary>
	/// <param name="json"> The JSON string. </param>
	/// <param name="settings"> The settings to be used. </param>
	/// <returns> The deserialized object. </returns>
	public static JsonValue Parse(string json, ISerializationOptions settings = null)
	{
		var consumer = new JsonValueJsonConsumer();
		using var reader = new StringReader(json);
		var tokenizer = new JsonTokenizer(reader);
		tokenizer.MoveNext();

		JsonParser.ParseValue(tokenizer, consumer, settings);

		if (tokenizer.CurrentToken.Type != JsonTokenType.None)
		{
			throw new ParserException($"Unexpected input data '{tokenizer.CurrentToken}' after end of JSON entity in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.Position}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.Position);
		}

		return consumer.Result;
	}

	/// <summary>
	/// Registers a converter.
	/// </summary>
	/// <param name="converter"> The converter to process. </param>
	public static void RegisterConverter(IJsonConverter converter)
	{
		if (_customConverters.Contains(converter))
		{
			return;
		}

		_customConverters.Add(converter);
	}

	/// <inheritdoc />
	public string ToJson<T>(T value, ISerializationOptions settings = null)
	{
		if (value == null)
		{
			return JsonNull.Value;
		}

		var consumer = _consumerProvider.Invoke(settings ?? Serializer.DefaultSettings);
		consumer.WriteObject(value);
		return consumer.ToString();
	}

	/// <inheritdoc />
	public string ToJson(object value, Type type, ISerializationOptions settings = null)
	{
		if (value == null)
		{
			return JsonNull.Value;
		}

		var consumer = _consumerProvider.Invoke(settings ?? Serializer.DefaultSettings);
		consumer.WriteObject(value);
		return consumer.ToString();
	}

	#endregion
}