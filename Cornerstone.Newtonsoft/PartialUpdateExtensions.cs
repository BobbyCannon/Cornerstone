#region References

using System;
using System.IO;
using System.Linq;
using Cornerstone.Convert;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Newtonsoft.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace Cornerstone.Newtonsoft;

/// <summary>
/// Extensions
/// </summary>
public static class PartialUpdateExtensions
{
	#region Methods

	/// <summary>
	/// Gets a partial update from a JSON string.
	/// </summary>
	/// <typeparam name="T"> The type the partial update is for. </typeparam>
	/// <param name="json"> The JSON containing the partial update. </param>
	/// <param name="options"> The options for the partial update. </param>
	/// <returns> The partial update. </returns>
	public static PartialUpdate<T> FromJson<T>(string json, IncludeExcludeOptions options = null)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return new PartialUpdate<T>();
		}

		return (PartialUpdate<T>) FromJson(typeof(T), json, options);
	}

	/// <summary>
	/// Gets a partial update from a JSON string.
	/// </summary>
	/// <param name="type"> The type the partial update is for. </param>
	/// <param name="json"> The JSON containing the partial update. </param>
	/// <param name="options"> The options for the partial update. </param>
	/// <returns> The partial update. </returns>
	public static PartialUpdate FromJson(Type type, string json, IncludeExcludeOptions options = null)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return CreatePartialUpdateInstance(type);
		}

		using var reader = new JsonTextReader(new StringReader(json));
		reader.Read();
		return FromJson(type, reader, options);
	}

	/// <summary>
	/// Gets a partial update from a JSON string.
	/// </summary>
	/// <param name="type"> The type the partial update is for. </param>
	/// <param name="reader"> The JSON containing the partial update. </param>
	/// <param name="options"> The options for the partial update. </param>
	/// <returns> The partial update. </returns>
	public static PartialUpdate FromJson(this Type type, JsonReader reader, IncludeExcludeOptions options = null)
	{
		var update = CreatePartialUpdateInstance(type);
		return LoadJson(update, reader, options);
	}

	private static PartialUpdate CreatePartialUpdateInstance(Type type)
	{
		if (type.IsSubclassOf(PartialUpdateConverter.TypeOfPartialUpdate))
		{
			return (PartialUpdate) type.CreateInstance();
		}

		return (PartialUpdate) typeof(PartialUpdate<>).CreateInstanceOfGeneric([type]);
	}

	private static PartialUpdate LoadJson(PartialUpdate partialUpdate, JsonReader reader, IncludeExcludeOptions options)
	{
		if (reader.TokenType == JsonToken.StartArray)
		{
			return partialUpdate;
		}

		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}

		var jObject = JObject.Load(reader);
		var jProperties = jObject.Properties();
		var directProperties = partialUpdate.GetType().GetCachedProperties();
		var targetProperties = partialUpdate.GetTargetProperties();

		foreach (var jProperty in jProperties)
		{
			if (!options.ShouldProcessProperty(jProperty.Name))
			{
				continue;
			}

			var directWritableProperty = directProperties.FirstOrDefault(x => string.Equals(x.Name, jProperty.Name, StringComparison.OrdinalIgnoreCase) && x.CanWrite);
			var targetProperty = targetProperties.FirstOrDefault(x => string.Equals(x.Key, jProperty.Name, StringComparison.OrdinalIgnoreCase)).Value;
			var targetPropertyType = directWritableProperty?.PropertyType
				?? targetProperty?.PropertyType
				?? PartialUpdateConverter.ConvertType(jProperty.Value.Type);

			if ((jProperty.Type == JTokenType.Null)
				|| (jProperty.Value.Type == JTokenType.Null))
			{
				// Only keep "null" values for nullable property types
				if (targetPropertyType.IsNullable())
				{
					partialUpdate.AddOrUpdate(jProperty.Name, targetPropertyType, null);
				}
				continue;
			}

			if (jProperty.Value.TryConvertTo(targetPropertyType, out var readValue))
			{
				var readValueType = readValue?.GetType();
				var readValueDefaultType = readValueType?.CreateInstance();

				if ((directWritableProperty != null)
					&& (readValueType != null)
					&& directWritableProperty.PropertyType.IsAssignableFrom(readValueType)
					&& !Equals(readValue, readValueDefaultType))
				{
					directWritableProperty.SetValue(partialUpdate, readValue);
				}
				else
				{
					partialUpdate.AddOrUpdate(jProperty.Name, targetPropertyType, readValue);
				}
			}
			else
			{
				partialUpdate.AddOrUpdate(jProperty.Name, targetPropertyType, jProperty.Value);
			}
		}

		partialUpdate.RefreshUpdates();

		return partialUpdate;
	}

	#endregion
}