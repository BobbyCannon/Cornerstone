#region References

using System;
using System.Collections.Generic;
using System.IO;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Json;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Consumers;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Storage;
using Cornerstone.Text.Buffers;

#endregion

namespace Cornerstone.Serialization.Json;

/// <inheritdoc />
public class JsonSerializer : IJsonSerializer
{
	#region Fields

	private static readonly MemoryCache<Type, IJsonConverter> _cache;
	private readonly Func<ISerializationSettings, IObjectConsumer> _consumerProvider;
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
	public JsonSerializer(IStringBuffer buffer)
		: this(x => new TextJsonConsumer(buffer, x))
	{
	}

	/// <summary>
	/// Initialize the JSON serializer.
	/// </summary>
	/// <param name="consumerProvider"> The consumer to use when serializing. </param>
	public JsonSerializer(Func<ISerializationSettings, IObjectConsumer> consumerProvider)
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
				new DataTableConverter(),
				new DateJsonConverter(),
				new DictionaryJsonConverter(),
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
		if (type.IsNullable() && jsonValue is JsonNull or null)
		{
			return null;
		}

		var converter = GetConverter(type);
		var response = converter.ConvertTo(type, jsonValue);
		(response as ITrackPropertyChanges)?.ResetHasChanges();
		return response;
	}

	/// <inheritdoc />
	public T FromJson<T>(string value, ISerializationSettings settings = null)
	{
		return (T) FromJson(value, typeof(T), settings);
	}

	/// <inheritdoc />
	public object FromJson(string value, Type type, ISerializationSettings settings = null)
	{
		var result = Parse(value, settings);
		var converter = GetConverter(type);
		var response = converter.ConvertTo(type, result);
		(response as ITrackPropertyChanges)?.ResetHasChanges();
		return response;
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
				_cache.AddOrUpdate(type, converter);
				return converter;
			}
		}

		foreach (var converter in _converters)
		{
			if (converter.CanConvert(type))
			{
				_cache.AddOrUpdate(type, converter);
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
	public static JsonValue Parse(string json, ISerializationSettings settings = null)
	{
		var consumer = new JsonValueJsonConsumer();
		using var reader = new StringReader(json);
		var tokenizer = new JsonTokenizer();
		tokenizer.Add(reader.ReadToEnd());
		tokenizer.ParseNext();

		JsonParser.ParseValue(tokenizer, consumer, settings);

		if (tokenizer.CurrentToken.Type != JsonTokenType.None)
		{
			throw new ParserException($"Unexpected input data '{tokenizer.CurrentToken}' after end of JSON entity in line {tokenizer.CurrentToken.LineNumber} at position {tokenizer.CurrentToken.ColumnNumber}.", tokenizer.CurrentToken.LineNumber, tokenizer.CurrentToken.ColumnNumber);
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
	public string ToJson<T>(T value, ISerializationSettings settings = null)
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
	public string ToJson(object value, Type type, ISerializationSettings settings = null)
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