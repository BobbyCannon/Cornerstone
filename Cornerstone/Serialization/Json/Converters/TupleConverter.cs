#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Consumers;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The tuple converter.
/// </summary>
public class TupleConverter : JsonConverter
{
	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationOptions settings)
	{
		if (!CanConvert(valueType))
		{
			throw new NotSupportedException("This value is not supported by this converter.");
		}

		if (value == null)
		{
			consumer.Null();
			return;
		}

		var values = value.GetValueTupleItemDictionary();
		var writer = consumer.StartObject(typeof(JsonObject));
		DictionaryConverter.WriteDictionary(writer, values, settings);
		consumer.CompleteObject();
	}

	/// <inheritdoc />
	public override bool CanConvert(Type type)
	{
		return type.IsTuple()
			|| type.IsValueTuple();
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
	public override string GetJsonString(object value, ISerializationOptions settings)
	{
		var consumer = new TextJsonConsumer(settings);
		Append(value, value?.GetType(), consumer, settings);
		return consumer.ToString();
	}

	#endregion
}