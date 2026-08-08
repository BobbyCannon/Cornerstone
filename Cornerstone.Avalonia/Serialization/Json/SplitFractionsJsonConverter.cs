#region References

using Cornerstone.Avalonia.DockingManager;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

#endregion

namespace Cornerstone.Avalonia.Serialization.Json;

public class SplitFractionsJsonConverter : JsonConverter<SplitFractions>
{
	#region Methods

	public override SplitFractions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}

		// Read the array of ints directly
		var fractions = JsonSerializer.Deserialize<int[]>(ref reader, options) ?? [];
		return new SplitFractions(fractions);
	}

	public override void Write(Utf8JsonWriter writer, SplitFractions value, JsonSerializerOptions options)
	{
		// Serialize as a simple int array for clean JSON
		JsonSerializer.Serialize(writer, value.ToArray(), options);
	}

	#endregion
}