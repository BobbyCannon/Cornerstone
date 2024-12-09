#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Cornerstone.Collections;
using Cornerstone.Compare;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Consumers;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The converter for all array types
/// </summary>
public class ArrayJsonConverter : JsonConverter
{
	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationSettings settings)
	{
		var arrayType = value.GetType();
		var arrayValue = (IEnumerable) value;
		var textBuilder = consumer as ITextBuilder;
		var referenceTracker = consumer as IReferenceTracker;
		consumer.StartObject(arrayType);
		var first = true;

		// todo: Get array type, maybe we can use a single converter

		foreach (var item in arrayValue)
		{
			if (referenceTracker?.AlreadyProcessed(item) == true)
			{
				continue;
			}

			if (!first)
			{
				consumer.WriteRawString(",");
				textBuilder?.NewLine();
			}

			if (item == null)
			{
				consumer.WriteRawString("null");
			}
			else
			{
				referenceTracker?.AddReference(item);

				var itemType = item.GetType();
				var converter = JsonSerializer.GetConverter(itemType);
				converter.Append(item, itemType, consumer, settings);
			}

			first = false;
		}

		textBuilder?.NewLine();
		consumer.CompleteObject();
	}

	/// <inheritdoc />
	public override bool CanConvert(Type type)
	{
		return IsArray(type);
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		if (type.IsNullable() && jsonValue is null or JsonNull)
		{
			return null;
		}

		switch (jsonValue)
		{
			case JsonArray jsonArray:
			{
				if (type.IsArray)
				{
					return ProcessArray(type, jsonArray);
				}

				if (type.ImplementsType<IList>()
					|| type.ImplementsType(typeof(IList<>))
					|| type.ImplementsType(typeof(ISpeedyList<>)))
				{
					return ProcessList(type, jsonArray);
				}

				if (type.ImplementsType<IEnumerable>()
					|| type.ImplementsType(typeof(IEnumerable<>)))
				{
					var arg = type.GetGenericArguments()[0];
					var listType = typeof(List<>);
					listType = listType.MakeGenericType(arg);
					return ProcessList(listType, jsonArray);
				}

				return ProcessArray(type, jsonArray);
			}
		}

		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationSettings settings)
	{
		var consumer = new TextJsonConsumer(settings);
		Append(value, value?.GetType(), consumer, settings);
		return consumer.ToString();
	}

	internal static bool IsArray(Type type)
	{
		if (type == typeof(JsonObject))
		{
			return false;
		}

		var info = type?.GetTypeInfo();
		return info is { IsArray: true }
			|| type.ImplementsType<IEnumerable>();
	}

	private object ProcessArray(Type arrayType, JsonArray jsonArray)
	{
		var arrayElementType = arrayType.GetElementType() 
			?? arrayType.GetGenericArgumentsRecursive()?.FirstOrDefault();

		arrayElementType ??= typeof(object);
		var response = Array.CreateInstance(arrayElementType, jsonArray.Count);
		var arrayElementTypeIsObject = arrayElementType == typeof(object);

		for (var i = 0; i < jsonArray.Count; i++)
		{
			var item = jsonArray[i];
			if (item == null)
			{
				response.SetValue(null, i);
				continue;
			}

			if (arrayElementTypeIsObject || (arrayElementType == item.GetType()))
			{
				response.SetValue(item, i);
				continue;
			}

			var converter = JsonSerializer.GetConverter(arrayElementType);
			var value = converter.ConvertTo(arrayElementType, item);
			response.SetValue(value, i);
		}

		return response;
	}

	private object ProcessList(Type arrayType, JsonArray jsonArray)
	{
		var response = (IList) arrayType.CreateInstance();
		var listElementType = arrayType.GetGenericArgumentsRecursive()?.FirstOrDefault() ?? typeof(object);
		var listElementTypeIsObject = listElementType == typeof(object);

		for (var i = 0; i < jsonArray.Count; i++)
		{
			var item = jsonArray[i];
			if (item is null or JsonNull)
			{
				response.Add(null);
				continue;
			}

			if (listElementTypeIsObject || (listElementType == item.GetType()))
			{
				response.Add(item);
				continue;
			}

			var converter = JsonSerializer.GetConverter(listElementType == typeof(DataTable)
				? typeof(DataTable)
				: item.GetType()
			);
			response.Add(converter.ConvertTo(listElementType, item));
		}

		return response;
	}

	#endregion
}