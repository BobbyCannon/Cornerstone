#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The converter for all number types. Supports the following types: Guid, ShortGuid
/// </summary>
public class GuidJsonConverter : JsonConverter
{
	#region Constructors

	/// <inheritdoc />
	public GuidJsonConverter() : base(typeof(Guid), typeof(ShortGuid))
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationOptions settings)
	{
		var t = value switch
		{
			Guid cValue => cValue.ToString(),
			ShortGuid cValue => cValue.Guid.ToString(),
			_ => throw new NotImplementedException()
		};

		consumer.String(t);
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		return jsonValue switch
		{
			JsonNull => Converter.ConvertTo(null, type),
			JsonString x => x.Value.ConvertTo(type),
			_ => throw new NotImplementedException()
		};
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationOptions settings)
	{
		var sValue = value switch
		{
			Guid g => g.ToString(),
			ShortGuid cValue => cValue.Guid.ToString(),
			_ => throw new NotImplementedException()
		};

		return "\"" + sValue + "\"";
	}

	#endregion
}