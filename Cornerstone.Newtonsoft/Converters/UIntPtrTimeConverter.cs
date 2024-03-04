#region References

using System;
using Cornerstone.Serialization.Json.Converters;
using Newtonsoft.Json;

#endregion

namespace Cornerstone.Newtonsoft.Converters;

/// <summary>
/// Converter used to deserialize JSON string to IntPtr.
/// </summary>
public class UIntPtrTimeConverter : JsonConverter<UIntPtr>
{
	#region Methods

	/// <inheritdoc />
	public override UIntPtr ReadJson(JsonReader reader, Type objectType, UIntPtr existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		return reader.Value is UIntPtr dValue
			? dValue
			#if NETSTANDARD
			: new UIntPtr(uint.Parse(reader.Value?.ToString()));
			#else
			: UIntPtr.Parse(reader.Value?.ToString());
			#endif
	}

	/// <inheritdoc />
	public override void WriteJson(JsonWriter writer, UIntPtr value, JsonSerializer serializer)
	{
		writer.WriteRawValue($"{NumberJsonConverter.ToJson(value)}");
	}

	#endregion
}