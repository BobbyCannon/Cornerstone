#region References

using System;
using Cornerstone.Serialization.Json.Converters;
using Newtonsoft.Json;

#endregion

namespace Cornerstone.Newtonsoft.Converters;

/// <summary>
/// Converter used to deserialize JSON string to IntPtr.
/// </summary>
public class IntPtrTimeConverter : JsonConverter<IntPtr>
{
	#region Methods

	/// <inheritdoc />
	public override IntPtr ReadJson(JsonReader reader, Type objectType, IntPtr existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		return reader.Value is IntPtr dValue
			? dValue
			#if NETSTANDARD
			: new IntPtr(int.Parse(reader.Value?.ToString()));
			#else
			: IntPtr.Parse(reader.Value?.ToString());
			#endif
	}

	/// <inheritdoc />
	public override void WriteJson(JsonWriter writer, IntPtr value, JsonSerializer serializer)
	{
		writer.WriteRawValue($"{NumberJsonConverter.ToJson(value)}");
	}

	#endregion
}