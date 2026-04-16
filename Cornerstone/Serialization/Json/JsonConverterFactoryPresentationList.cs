#region References

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cornerstone.Collections;
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

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var itemType = typeToConvert.GetGenericArguments()[0];
		var converterType = typeof(JsonConverterForPresentationList<>).MakeGenericType(itemType);
		return (JsonConverter) SourceReflector.CreateInstance(converterType)!;
	}

	#endregion
}