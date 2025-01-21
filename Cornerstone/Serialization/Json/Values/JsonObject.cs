#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Comparer = Cornerstone.Compare.Comparer;

#endregion

namespace Cornerstone.Serialization.Json.Values;

/// <summary>
/// This class represents a generic JSON object.
/// </summary>
public class JsonObject : JsonValue, IReadOnlyDictionary<string, JsonValue>, IObjectConsumer
{
	#region Fields

	private readonly Dictionary<string, JsonValue> _properties;
	private string _propertyName;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an empty JSON object.
	/// </summary>
	public JsonObject()
	{
		_properties = new Dictionary<string, JsonValue>();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public int Count => _properties.Count;

	/// <inheritdoc />
	public JsonValue this[string key] => _properties[key];

	/// <inheritdoc />
	public IEnumerable<string> Keys => _properties.Keys;

	/// <inheritdoc />
	public IEnumerable<JsonValue> Values => _properties.Values;

	#endregion

	#region Methods

	/// <inheritdoc />
	public bool AlreadyProcessed(object value)
	{
		return false;
	}

	/// <inheritdoc />
	public IObjectConsumer Boolean(bool value)
	{
		_properties.AddOrUpdate(_propertyName, new JsonBoolean(value));
		return this;
	}

	/// <inheritdoc />
	public override int CompareTo(object obj)
	{
		if (obj is JsonObject jObject)
		{
			return Comparer.Compare(_properties, jObject._properties).AreEqual() ? 0 : 1;
		}

		return -1;
	}

	/// <inheritdoc />
	public IObjectConsumer CompleteObject()
	{
		return this;
	}

	/// <inheritdoc />
	public bool ContainsKey(string key)
	{
		return _properties.ContainsKey(key);
	}

	/// <inheritdoc />
	public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator()
	{
		return _properties.GetEnumerator();
	}

	/// <inheritdoc />
	public IObjectConsumer Null()
	{
		_properties.AddOrUpdate(_propertyName, new JsonNull());
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer Number(object value)
	{
		_properties.AddOrUpdate(_propertyName, new JsonNumber(value));
		return this;
	}

	/// <summary>
	/// Start a property write.
	/// </summary>
	/// <param name="propertyName"> The property. </param>
	public IObjectConsumer PropertyName(string propertyName)
	{
		_propertyName = propertyName;
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer StartObject(Type type)
	{
		if (type.IsArray || (type == typeof(JsonArray)))
		{
			var array = new JsonArray();
			_properties.AddOrUpdate(_propertyName, array);
			return array;
		}

		var jObject = new JsonObject();
		_properties.AddOrUpdate(_propertyName, jObject);
		return jObject;
	}

	/// <inheritdoc />
	public IObjectConsumer String(string value)
	{
		_properties.AddOrUpdate(_propertyName, new JsonString(value));
		return this;
	}

	/// <inheritdoc />
	public bool TryGetValue(string key, out JsonValue value)
	{
		return _properties.TryGetValue(key, out value);
	}

	/// <inheritdoc />
	public IObjectConsumer WriteObject(object value)
	{
		if (value is JsonValue jValue)
		{
			_properties.AddOrUpdate(_propertyName, () => jValue, _ => jValue);
			return this;
		}

		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public IObjectConsumer WriteProperty(string name, object value)
	{
		PropertyName(name);
		WriteObject(value);
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer WriteProperty(PropertyInfo info, object value)
	{
		PropertyName(info.Name);
		WriteObject(value);
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer WriteRawString(string value)
	{
		return this;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}