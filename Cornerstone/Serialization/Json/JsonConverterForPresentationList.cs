#region References

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Serialization.Json;

public class JsonConverterForPresentationList<T> : JsonConverter<IPresentationList<T>>
{
	#region Methods

	public override IPresentationList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return JsonSerializer.Deserialize<PresentationList<T>>(ref reader, options);
	}

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