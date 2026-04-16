#region References

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Serialization.Json;

/// <summary>
/// Factory remains unchanged – it is already optimal and called only once per type.
/// No Activator reflection in the hot path.
/// </summary>
public class JsonConverterFactoryPartialUpdate : JsonConverterFactory
{
	#region Methods

	public override bool CanConvert(Type typeToConvert)
	{
		if (typeToConvert == typeof(PartialUpdate))
		{
			return true;
		}

		if (!typeToConvert.IsGenericType)
		{
			return false;
		}

		var definition = typeToConvert.GetGenericTypeDefinition();
		return definition == typeof(PartialUpdate<>);
	}

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		// Fast path for the non-generic case
		if (typeToConvert == typeof(PartialUpdate))
		{
			return JsonConverterForPartialUpdate.Instance;
		}

		var itemType = typeToConvert.GetGenericArguments()[0];
		var converterType = typeof(JsonConverterForPartialUpdate<>).MakeGenericType(itemType);
		return (JsonConverter) SourceReflector.CreateInstance(converterType)!;
	}

	#endregion
}