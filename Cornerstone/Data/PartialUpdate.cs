#region References

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Data;

public class PartialUpdate<T> : PartialUpdate
{
	#region Methods

	public new static PartialUpdate<T> FromDictionary(Dictionary<string, JsonElement> dictionary)
	{
		if (dictionary is null || (dictionary.Count == 0))
		{
			return new PartialUpdate<T>();
		}

		var partial = new PartialUpdate<T>();

		foreach (var kvp in dictionary)
		{
			var name = kvp.Key;
			var jsonElement = kvp.Value;

			// Now using the centralized, zero-alloc cache from SourceReflector
			if (SourceReflector.GetPropertyTypes<T>().TryGetValue(name, out var expectedType))
			{
				var value = (expectedType == typeof(object))
					|| (expectedType == typeof(JsonElement))
						? jsonElement
						: jsonElement.Deserialize(expectedType, Serializer.SerializationOptions);

				partial.AddOrUpdate(name, expectedType, value);
			}
			else
			{
				var value = ConvertJsonElementToObject(jsonElement);
				var runtimeType = value?.GetType() ?? typeof(object);
				partial.AddOrUpdate(name, runtimeType, value);
			}
		}

		return partial;
	}

	public new static PartialUpdate<T> FromJsonElement(JsonElement element)
	{
		if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
		{
			return null;
		}

		var partial = new PartialUpdate<T>();

		// Uses the zero-alloc cache you just added to SourceReflector
		var propertyTypes = SourceReflector.GetPropertyTypes<T>();

		foreach (var prop in element.EnumerateObject())
		{
			var name = prop.Name;
			var jsonElement = prop.Value;

			if (propertyTypes.TryGetValue(name, out var expectedType))
			{
				var value = (expectedType == typeof(object)) || (expectedType == typeof(JsonElement))
					? jsonElement
					: jsonElement.Deserialize(expectedType, Serializer.SerializationOptions);

				partial.AddOrUpdate(name, expectedType, value);
			}
			else
			{
				var value = ConvertJsonElementToObject(jsonElement);
				var runtimeType = value?.GetType() ?? typeof(object);
				partial.AddOrUpdate(name, runtimeType, value);
			}
		}

		return partial;
	}

	#endregion
}

[SourceReflection]
public class PartialUpdate : Bindable
{
	#region Fields

	// Changed to Dictionary – much faster than SortedDictionary
	internal readonly Dictionary<string, PartialUpdateValue> Updates;

	private SourcePropertyInfo[] _properties;

	// Static cache for the base class's own properties (used only by the 2-param AddOrUpdate)
	private static readonly Dictionary<Type, SourcePropertyInfo[]> _propertyCache = new();

	#endregion

	#region Constructors

	public PartialUpdate()
	{
		Updates = new Dictionary<string, PartialUpdateValue>(StringComparer.OrdinalIgnoreCase);
	}

	#endregion

	#region Methods

	public void AddOrUpdate(string name, object value)
	{
		// Fast path: get properties for the concrete type (cached)
		if (_properties is null)
		{
			var type = GetType();
			if (!_propertyCache.TryGetValue(type, out _properties))
			{
				_properties = SourceReflector.GetRequiredSourceType(type).GetProperties();
				_propertyCache[type] = _properties;
			}
		}

		// Manual loop – no LINQ enumerator
		SourcePropertyInfo property = null;
		for (var i = 0; i < _properties.Length; i++)
		{
			if (string.Equals(_properties[i].Name, name, StringComparison.OrdinalIgnoreCase))
			{
				property = _properties[i];
				break;
			}
		}

		var propertyType = value == null
			? property?.PropertyInfo.PropertyType ?? typeof(object)
			: value.GetType();

		AddOrUpdate(name, propertyType, value);
	}

	public void AddOrUpdate(string name, Type type, object value)
	{
		if (!Updates.TryGetValue(name, out var update))
		{
			Updates.Add(name, new PartialUpdateValue(name, type, value));
			return;
		}

		// Rare case: key casing differs (ignore-case dictionary already normalizes lookup)
		if (!string.Equals(update.Name, name, StringComparison.Ordinal))
		{
			Updates.Remove(update.Name);
			update.Name = name;
			Updates.Add(update.Name, update);
		}

		if (value == null)
		{
			update.Value = null;
			return;
		}

		var valueType = value.GetType();
		var isExactType = update.Type == valueType;
		var inherits = valueType.ImplementsType(update.Type);

		update.Value = isExactType || inherits
			? value
			: value.ConvertTo(update.Type);
	}

	public static PartialUpdate FromDictionary(Dictionary<string, JsonElement> dictionary)
	{
		if (dictionary is null || (dictionary.Count == 0))
		{
			return new PartialUpdate();
		}

		var partial = new PartialUpdate();

		foreach (var kvp in dictionary)
		{
			var value = ConvertJsonElementToObject(kvp.Value);
			var type = value?.GetType() ?? typeof(object);
			partial.AddOrUpdate(kvp.Key, type, value);
		}

		return partial;
	}

	// Inside PartialUpdate (base class)
	public static PartialUpdate FromJsonElement(JsonElement element)
	{
		if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
		{
			return null;
		}

		var partial = new PartialUpdate();

		foreach (var prop in element.EnumerateObject()) // zero-allocation struct enumerator
		{
			var value = ConvertJsonElementToObject(prop.Value);
			var type = value?.GetType() ?? typeof(object);
			partial.AddOrUpdate(prop.Name, type, value);
		}

		return partial;
	}

	/// <summary>
	/// Returns a frozen (immutable, zero-allocation after creation) view.
	/// Call once after building the partial update.
	/// </summary>
	public virtual IReadOnlyDictionary<string, object> ToDictionary()
	{
		// Project to object and freeze – .NET 8+ / 10+ gives us a truly immutable dictionary
		return Updates.ToFrozenDictionary(
			kvp => kvp.Key,
			kvp => kvp.Value.GetValue(),
			StringComparer.OrdinalIgnoreCase);
	}

	public bool TryGet<T>(out T value, [CallerMemberName] string name = "")
	{
		if (Updates.TryGetValue(name, out var update))
		{
			return update.Value.TryConvertTo(out value);
		}

		value = default;
		return false;
	}

	public bool TryUpdate<T>(Action<T> update, [CallerMemberName] string name = "")
	{
		if (!TryGet(out T value, name))
		{
			return false;
		}

		update.Invoke(value);
		return true;
	}

	/// <summary>
	/// Fast-path conversion for the most common JSON kinds.
	/// Avoids the full Deserialize object serializer for primitives.
	/// </summary>
	internal static object ConvertJsonElementToObject(JsonElement element)
	{
		return element.ValueKind switch
		{
			JsonValueKind.Null or JsonValueKind.Undefined => null,

			JsonValueKind.String => element.GetString(),

			JsonValueKind.Number => element.TryGetInt64(out var l) ? l :
				element.TryGetDouble(out var d) ? d :
				element.GetDecimal(),

			JsonValueKind.True => true,
			JsonValueKind.False => false,

			// For complex types we still need the serializer (unavoidable)
			JsonValueKind.Array or JsonValueKind.Object => element.Deserialize<object>(),

			_ => element.Deserialize<object>()
		};
	}

	#endregion
}