#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cornerstone.Presentation;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Serialization.Json;

public class JsonConverterFactoryPresentationList : JsonConverterFactory
{
	#region Methods

	public override bool CanConvert(Type typeToConvert)
	{
		if (!typeToConvert.IsGenericType)
		{
			return false;
		}

		var definition = typeToConvert.GetGenericTypeDefinition();
		return definition == typeof(IPresentationList<>);
	}

	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "JsonConverterFactory.CreateConverter cannot be annotated; closed generic is created for PresentationList<T>.")]
	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var itemType = typeToConvert.GetGenericArguments()[0];
		var converterType = typeof(JsonConverterForPresentationList<>).MakeGenericType(itemType);
		return (JsonConverter) SourceReflector.CreateInstance(converterType)!;
	}

	#endregion
}