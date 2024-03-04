#region References

using System;
using Newtonsoft.Json;

#endregion

namespace Cornerstone.Newtonsoft.Converters;

/// <summary>
/// Converter used to deserialize JSON string to ShortGuid.
/// </summary>
public class ShortGuidConverter : JsonConverter<ShortGuid>
{
	#region Methods

	/// <inheritdoc />
	public override ShortGuid ReadJson(JsonReader reader, Type objectType, ShortGuid existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		return reader.Value is ShortGuid dValue
			? dValue
			: ShortGuid.Parse(reader.Value?.ToString());
	}

	/// <inheritdoc />
	public override void WriteJson(JsonWriter writer, ShortGuid value, JsonSerializer serializer)
	{
		writer.WriteRawValue($"\"{value.Guid}\"");
	}

	#endregion
}