#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The converter for all enum types
/// </summary>
public class EnumJsonConverter : JsonConverter
{
	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationOptions settings)
	{
		consumer.WriteRawString(GetJsonString(value, settings));
	}

	/// <inheritdoc />
	public override bool CanConvert(Type type)
	{
		if (type.IsNullableType())
		{
			type = type.FromNullableType();
		}
		return type.IsEnum;
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		var sValue = jsonValue switch
		{
			JsonNull => Converter.ConvertTo(null, type),
			JsonString x => x.Value.ConvertTo(type),
			JsonNumber x => x.Value.ConvertTo(type),
			_ => jsonValue.ConvertTo(type)
		};

		return sValue;
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationOptions settings)
	{
		if (settings.EnumFormat == EnumFormat.Value)
		{
			return ((Enum) value).ToString("D");
		}

		var sValue = value?.ToString();
		return "\"" + (sValue.Escape() ?? JsonNull.Value) + "\"";
	}

	#endregion
}