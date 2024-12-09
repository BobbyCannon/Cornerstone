#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The converter for all partial update values.
/// </summary>
public class PartialUpdateValueJsonConverter : JsonConverter
{
	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationSettings settings)
	{
		consumer.WriteRawString(GetJsonString(value, settings));
	}

	/// <inheritdoc />
	public override bool CanConvert(Type type)
	{
		return type == typeof(PartialUpdateValue);
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		var sValue = jsonValue switch
		{
			JsonArray x => x.ConvertTo(type),
			JsonObject x => x.ConvertTo(type),
			_ => jsonValue.ConvertTo(type)
		};

		return sValue;
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationSettings settings)
	{
		var pValue = (PartialUpdateValue) value;
		return pValue.Value?.ToJson();
	}

	#endregion
}