#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The converter for Boolean.
/// </summary>
public class BooleanJsonConverter : JsonConverter
{
	#region Constructors

	/// <inheritdoc />
	public BooleanJsonConverter() : base(typeof(bool), typeof(bool?))
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationSettings settings)
	{
		if (value is bool bValue)
		{
			consumer.Boolean(bValue);
			return;
		}

		throw new NotSupportedException("This value is not supported by this converter.");
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		return jsonValue switch
		{
			JsonNull => Converter.ConvertTo(null, type),
			JsonBoolean x => x.Value.ConvertTo(type),
			JsonString x => x.Value.ConvertTo(type),
			_ => jsonValue.ConvertTo(type)
		};
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationSettings settings)
	{
		if (value is bool bValue)
		{
			return bValue ? "true" : "false";
		}

		throw new NotSupportedException("This value is not supported by this converter.");
	}

	#endregion
}