#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The converter for all string types. Supports the following types: String, StringBuilder
/// </summary>
public class StringJsonConverter : JsonConverter
{
	#region Constructors

	/// <inheritdoc />
	public StringJsonConverter() : base(
		ArrayExtensions.CombineArrays(
			Activator.StringTypes,
			Activator.CharTypes
		))
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationOptions settings)
	{
		consumer.WriteRawString(GetJsonString(value, settings));
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		return jsonValue switch
		{
			JsonNull => null,
			JsonString x => x.Value.ConvertTo(type),
			_ => throw new NotImplementedException()
		};
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationOptions settings)
	{
		if (value is JsonString jString)
		{
			value = jString.Value;
		}

		var sValue = value?.ToString();
		return sValue == null
			? JsonNull.Value
			: JsonString.EscapeWithQuotes(sValue);
	}

	#endregion
}