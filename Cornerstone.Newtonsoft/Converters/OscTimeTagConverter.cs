#region References

using System;
using Cornerstone.Protocols.Osc;
using Newtonsoft.Json;

#endregion

namespace Cornerstone.Newtonsoft.Converters;

/// <summary>
/// Converter used to deserialize JSON string to OscTimeTag.
/// </summary>
public class OscTimeTagConverter : JsonConverter<OscTimeTag>
{
	#region Methods

	/// <inheritdoc />
	public override OscTimeTag ReadJson(JsonReader reader, Type objectType, OscTimeTag existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		return reader.Value is DateTime dValue
			? new OscTimeTag(dValue)
			: OscTimeTag.Parse(reader.Value as string);
	}

	/// <inheritdoc />
	public override void WriteJson(JsonWriter writer, OscTimeTag value, JsonSerializer serializer)
	{
		writer.WriteRawValue($"\"{value}\"");
	}

	#endregion
}