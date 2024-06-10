#region References

using System;
using Cornerstone.Serialization.Json.Converters;
using Newtonsoft.Json;

#endregion

namespace Cornerstone.Newtonsoft.Converters;

#if NET7_0_OR_GREATER

/// <summary>
/// Converter used to deserialize JSON string to IntPtr.
/// </summary>
public class Int128TimeConverter : JsonConverter<Int128>
{
	#region Methods

	/// <inheritdoc />
	public override Int128 ReadJson(JsonReader reader, Type objectType, Int128 existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		return reader.Value is Int128 dValue ? dValue : Int128.Parse(reader.Value?.ToString());
	}

	/// <inheritdoc />
	public override void WriteJson(JsonWriter writer, Int128 value, JsonSerializer serializer)
	{
		writer.WriteRawValue($"{NumberJsonConverter.ToJson(value)}");
	}

	#endregion
}

/// <summary>
/// Converter used to deserialize JSON string to IntPtr.
/// </summary>
public class UInt128TimeConverter : JsonConverter<UInt128>
{
	#region Methods

	/// <inheritdoc />
	public override UInt128 ReadJson(JsonReader reader, Type objectType, UInt128 existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		return reader.Value is UInt128 dValue ? dValue : UInt128.Parse(reader.Value?.ToString());
	}

	/// <inheritdoc />
	public override void WriteJson(JsonWriter writer, UInt128 value, JsonSerializer serializer)
	{
		writer.WriteRawValue($"{NumberJsonConverter.ToJson(value)}");
	}

	#endregion
}

#endif