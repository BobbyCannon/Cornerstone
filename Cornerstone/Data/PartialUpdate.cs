#region References

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
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

				partial.Set(name, expectedType, value);
			}
			else
			{
				var value = ConvertJsonElementToObject(jsonElement);
				var runtimeType = value?.GetType() ?? typeof(object);
				partial.Set(name, runtimeType, value);
			}
		}

		return partial;
	}

	public static PartialUpdate<T> FromJsonElement(JsonElement element)
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

				partial.Set(name, expectedType, value);
			}
			else
			{
				var value = ConvertJsonElementToObject(jsonElement);
				var runtimeType = value?.GetType() ?? typeof(object);
				partial.Set(name, runtimeType, value);
			}
		}

		return partial;
	}

	#endregion
}

[SourceReflection]
public class PartialUpdate : CornerstoneObject
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
			partial.Set(kvp.Key, type, value);
		}

		return partial;
	}

	public static PartialUpdate FromJsonElement(Type toType, JsonElement element)
	{
		if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
		{
			return null;
		}

		if (SourceReflector.CreateInstance(toType) is not PartialUpdate partial)
		{
			return null;
		}

		// zero-allocation struct enumerator
		foreach (var prop in element.EnumerateObject())
		{
			var value = ConvertJsonElementToObject(prop.Value);
			var type = value?.GetType() ?? typeof(object);
			partial.Set(prop.Name, type, value);
		}

		return partial;
	}

	public T Get<T>(string name, T defaultValue = default)
	{
		if (Updates.TryGetValue(name, out var update)
			&& update.Value.TryConvertTo<T>(out var value))
		{
			return value;
		}

		return defaultValue;
	}

	public T GetProperty<T>(T defaultValue = default, [CallerMemberName] string name = "")
	{
		return Get(name, defaultValue);
	}

	public void Load(params PartialUpdateValue[] updates)
	{
		foreach (var update in updates)
		{
			Updates.AddOrUpdate(update.Name, update);
		}
	}

	public void Set<T>(string name, T value)
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

		Set(name, propertyType, value);
	}

	public void Set(string name, Type type, object value)
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

	public void SetProperty<T>(T value, [CallerMemberName] string name = "")
	{
		Set(name, value);
	}

	/// <summary>
	/// Returns a frozen (immutable, zero-allocation after creation) view.
	/// Call once after building the partial update.
	/// </summary>
	public virtual IReadOnlyDictionary<string, object> ToDictionary()
	{
		RefreshUpdates();
		return Updates.ToFrozenDictionary(
			kvp => kvp.Key,
			kvp => kvp.Value.GetValue(),
			StringComparer.OrdinalIgnoreCase
		);
	}

	public bool TryGet<T>(string name, out T value)
	{
		if (Updates.TryGetValue(name, out var update))
		{
			return update.Value.TryConvertTo(out value);
		}

		value = default;
		return false;
	}

	public bool TryGetProperty<T>(out T value, [CallerMemberName] string name = "")
	{
		return TryGet(name, out value);
	}

	public bool TrySet<T>(string name, Action<T> update)
	{
		if (!TryGet(name, out T value))
		{
			return false;
		}

		update.Invoke(value);
		return true;
	}

	public bool TrySetProperty<T>(Action<T> update, [CallerMemberName] string name = "")
	{
		return TrySet(name, update);
	}

	/// <summary>
	/// Refresh the update collection for this partial update.
	/// </summary>
	protected internal virtual void RefreshUpdates()
	{
		var options = GetDefaultIncludedProperties(UpdateableAction.PartialUpdate);
		var properties = SourceReflector.GetSourceType(GetType()).GetProperties();

		foreach (var option in options)
		{
			var property = properties.FirstOrDefault(x => x.Name == option);
			if (property is not { CanRead: true })
			{
				continue;
			}

			var value = property.GetValue(this);
			Set(property.Name, property.PropertyInfo.PropertyType, value);
		}
	}

	/// <summary>
	/// Reconcile the update collections.
	/// </summary>
	/// <param name="partialUpdate"> The partial update to reconcile with. </param>
	protected void Reconcile(PartialUpdate partialUpdate)
	{
		Updates.Reconcile(partialUpdate.Updates);
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
			JsonValueKind.Number => element.TryGetInt64(out var l)
				? l
				: element.TryGetDouble(out var d)
					? d
					: element.GetDecimal(),
			JsonValueKind.True => true,
			JsonValueKind.False => false,
			_ => null
		};
	}

	#endregion
}