#region References

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid;

public sealed class IndexPathJsonConverter : JsonConverter<IndexPath>
{
	#region Methods

	public override IndexPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartArray)
		{
			throw new JsonException("Expected JSON array for IndexPath.");
		}

		var indexes = new List<int>();

		while (reader.Read() && (reader.TokenType != JsonTokenType.EndArray))
		{
			if (reader.TokenType == JsonTokenType.Number)
			{
				indexes.Add(reader.GetInt32());
			}
			else if (reader.TokenType == JsonTokenType.Null)
			{
				throw new JsonException("IndexPath does not support null values.");
			}
		}

		return new IndexPath(indexes.ToArray());
	}

	public override void Write(Utf8JsonWriter writer, IndexPath value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();

		foreach (var index in value)
		{
			writer.WriteNumberValue(index);
		}

		writer.WriteEndArray();
	}

	#endregion
}