#region References

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Cornerstone.Attributes;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Consumers;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The converter for all class / struct types
/// </summary>
public class ObjectJsonConverter : JsonConverter
{
	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationSettings settings)
	{
		if (value == null)
		{
			consumer.Null();
			return;
		}

		// Convert to a new object.
		var firstProperty = true;
		var propertyInfos = valueType.GetCachedProperties();
		var objectConsumer = consumer.StartObject(valueType);
		var textBuilder = objectConsumer as ITextBuilder;
		var referenceTracker = objectConsumer as IReferenceTracker;
		var serializedModel = valueType.GetCachedCustomAttribute<SerializedModelAttribute>();
		var updateableOptions = value is IUpdateableOptionsProvider provider
			? provider.GetUpdateableOptions(settings.UpdateableAction)
			: null;

		if (serializedModel != null)
		{
			// Limit to the serialized model expected members
			propertyInfos = propertyInfos
				.Where(x => serializedModel.Expected.Contains(x.Name))
				.ToList();
		}

		for (var index = 0; index < propertyInfos.Count; index++)
		{
			var info = propertyInfos[index];

			if ((updateableOptions != null)
				&& !updateableOptions.ShouldProcessProperty(info.Name))
			{
				continue;
			}

			var attribute = info.GetCustomAttribute<SerializationIgnore>();
			if (attribute != null)
			{
				continue;
			}

			if (!info.CanWrite && info.CanRead && settings.IgnoreReadOnly)
			{
				continue;
			}

			if (info.IsIndexer())
			{
				// Property is an indexer
				continue;
			}

			var propertyValue = info.GetValue(value);
			if ((propertyValue == null) && settings.IgnoreNullValues)
			{
				continue;
			}

			if (settings.IgnoreDefaultValues && propertyValue.IsDefaultValue())
			{
				continue;
			}

			if (referenceTracker?.AlreadyProcessed(propertyValue) == true)
			{
				continue;
			}

			if (!firstProperty)
			{
				consumer.WriteRawString(",");
				textBuilder?.NewLine();
			}

			objectConsumer.WriteProperty(info.Name, propertyValue);
			firstProperty = false;
		}

		textBuilder?.NewLine();

		consumer.CompleteObject();
	}

	/// <inheritdoc />
	public override bool CanConvert(Type type)
	{
		return true;
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		// Convert to a new object.
		var response = type.CreateInstance();
		var propertyInfos = type.GetCachedPropertyDictionary();
		var jsonPropertyNames = type.GetCachedJsonPropertyNameDictionary();

		switch (jsonValue)
		{
			case JsonObject jsonObject:
			{
				if (response is IJsonParseable parseable)
				{
					parseable.Parse(jsonObject);
					return parseable;
				}

				foreach (var objectValue in jsonObject)
				{
					if (!propertyInfos.TryGetValue(objectValue.Key, out var info)
						&& !jsonPropertyNames.TryGetValue(objectValue.Key, out info))
					{
						continue;
					}

					var propertyType = info.PropertyType;
					object existingValue = null;

					if (info.CanRead)
					{
						// todo: Use existing provided values?
						existingValue = info.GetValue(response);
						if (existingValue != null)
						{
							propertyType = existingValue.GetType();
						}
					}

					var incomingValue = JsonSerializer.Convert(propertyType, objectValue.Value);

					if (!info.CanWrite)
					{
						// Add collection reconciliation support
						if (existingValue is IList existingList
							&& incomingValue is IEnumerable incomingList)
						{
							incomingList.ForEach(x => existingList.Add(x));
						}

						// Add IUpdateable support for read only support

						continue;
					}

					info.SetValue(response, incomingValue);
				}
				break;
			}
		}

		return response;
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationSettings settings)
	{
		var consumer = new TextJsonConsumer(settings);
		Append(value, value?.GetType(), consumer, settings);
		return consumer.ToString();
	}

	#endregion
}