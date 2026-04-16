#region References

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Serialization.Json;

/// <summary>
/// Generic converter – now zero extra allocations beyond the required JsonDocument.
/// Uses the new FromJsonElement path that skips the intermediate Dictionary entirely.
/// </summary>
public class JsonConverterForPartialUpdate<T> : JsonConverter<PartialUpdate<T>>
{
	#region Properties

	public static JsonConverterForPartialUpdate<T> Instance { get; } = new();

	#endregion

	#region Methods

	public override PartialUpdate<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var doc = JsonDocument.ParseValue(ref reader);
		var element = doc.RootElement;
		return element.ValueKind == JsonValueKind.Null ? null : PartialUpdate<T>.FromJsonElement(element);
	}

	public override void Write(Utf8JsonWriter writer, PartialUpdate<T> value, JsonSerializerOptions options)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}

		JsonSerializer.Serialize(writer, value.ToDictionary(), options);
	}

	#endregion
}

/// <summary>
/// Non-generic converter – same zero-extra-allocation treatment.
/// </summary>
public class JsonConverterForPartialUpdate : JsonConverter<PartialUpdate>
{
	#region Properties

	public static JsonConverterForPartialUpdate Instance { get; } = new();

	#endregion

	#region Methods

	public override PartialUpdate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var doc = JsonDocument.ParseValue(ref reader);
		var element = doc.RootElement;
		return element.ValueKind == JsonValueKind.Null ? null : PartialUpdate.FromJsonElement(element);
	}

	public override void Write(Utf8JsonWriter writer, PartialUpdate value, JsonSerializerOptions options)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}

		JsonSerializer.Serialize(writer, value.ToDictionary(), options);
	}

	#endregion
}