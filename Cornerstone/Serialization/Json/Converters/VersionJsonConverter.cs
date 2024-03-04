#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The converter for Version.
/// </summary>
public class VersionJsonConverter : JsonConverter
{
	#region Constructors

	/// <inheritdoc />
	public VersionJsonConverter() : base(typeof(Version))
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationOptions settings)
	{
		switch (value)
		{
			case Version version:
			{
				consumer.String(version.ToString());
				return;
			}
			default:
			{
				throw new NotSupportedException("This value is not supported by this converter.");
			}
		}
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		return jsonValue switch
		{
			JsonNull => Converter.ConvertTo(null, type),
			JsonString x => x.Value.ConvertTo(type),
			_ => jsonValue.ConvertTo(type)
		};
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationOptions settings)
	{
		return value switch
		{
			null => "null",
			Version version => $"\"{version}\"",
			_ => throw new NotSupportedException("This value is not supported by this converter.")
		};
	}

	#endregion
}