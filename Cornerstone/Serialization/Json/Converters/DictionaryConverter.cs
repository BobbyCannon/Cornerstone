#region References

using System;
using System.Collections;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Consumers;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The dictionary converter.
/// </summary>
public class DictionaryConverter : JsonConverter
{
	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationSettings settings)
	{
		if (value == null)
		{
			consumer.Null();
			return;
		}

		if (value is IDictionary dValue)
		{
			var writer = consumer.StartObject(typeof(JsonObject));
			WriteDictionary(writer, dValue, settings);
			consumer.CompleteObject();
			return;
		}

		throw new NotSupportedException("This value is not supported by this converter.");
	}

	/// <inheritdoc />
	public override bool CanConvert(Type type)
	{
		return type.ImplementsType(typeof(IDictionary));
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		return jsonValue switch
		{
			JsonNull => Converter.ConvertTo(null, type),
			JsonArray x => x.ConvertTo(type),
			JsonObject x => x.ConvertTo(type),
			_ => throw new NotImplementedException()
		};
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationSettings settings)
	{
		var consumer = new TextJsonConsumer(settings);
		Append(value, value?.GetType(), consumer, settings);
		return consumer.ToString();
	}

	/// <summary>
	/// Convert the dictionary to JSON.
	/// </summary>
	/// <param name="dictionary"> The dictionary to serialize. </param>
	/// <param name="settings"> The serializer settings. </param>
	/// <returns> The dictionary in JSON format. </returns>
	public static string ToJson(IDictionary dictionary, ISerializationSettings settings)
	{
		var consumer = new TextJsonConsumer(settings);
		WriteDictionary(consumer, dictionary, settings);
		return consumer.ToString();
	}

	/// <summary>
	/// Write the dictionary to a serializer consumer.
	/// </summary>
	/// <param name="consumer"> The consumer to write to. </param>
	/// <param name="dictionary"> The dictionary to write. </param>
	/// <param name="settings"> </param>
	public static void WriteDictionary(IObjectConsumer consumer, IDictionary dictionary, ISerializationSettings settings)
	{
		var keys = dictionary.Keys.ToObjectArray();
		var textBuilder = consumer as ITextBuilder;
		var firstProperty = true;

		for (var index = 0; index < keys.Length; index++)
		{
			var key = keys[index];
			var value = dictionary[key];

			if (((value == null) && settings.IgnoreNullValues)
				|| ((value == default) && settings.IgnoreDefaultValues))
			{
				continue;
			}

			if (!firstProperty)
			{
				consumer.WriteRawString(",");
				textBuilder?.NewLine();
			}

			consumer.WriteProperty(key.ToString(), value);
			firstProperty = false;
		}

		textBuilder?.NewLine();
	}

	#endregion
}