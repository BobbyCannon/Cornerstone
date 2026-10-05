#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Serialization.Json;

public class JsonConverterForPresentationList<T> : JsonConverter<IPresentationList<T>>
{
	#region Methods

	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "JsonConverter.Read cannot be annotated; uses JsonSerializer.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "JsonConverter.Read cannot be annotated; uses JsonSerializer.")]
	public override IPresentationList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return JsonSerializer.Deserialize<PresentationList<T>>(ref reader, options);
	}

	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "JsonConverter.Write cannot be annotated; uses JsonSerializer.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "JsonConverter.Write cannot be annotated; uses JsonSerializer.")]
	public override void Write(Utf8JsonWriter writer, IPresentationList<T> value, JsonSerializerOptions options)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}

		JsonSerializer.Serialize(writer, value.ToList(), options);
	}

	#endregion
}