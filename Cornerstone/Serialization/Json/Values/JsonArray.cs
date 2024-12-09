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
/// The class represents a generic JSON array.
/// </summary>
public class JsonArray : JsonValue, IReadOnlyList<JsonValue>, IObjectConsumer
{
	#region Fields

	private readonly IList<JsonValue> _list;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an empty array.
	/// </summary>
	public JsonArray()
	{
		_list = new List<JsonValue>();
	}
	
	/// <summary>
	/// Initializes an array of default values
	/// </summary>
	public JsonArray(params JsonValue[] values): this()
	{
		_list.Add(values);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the number of elements contained in this JSON array.
	/// </summary>
	public int Count => _list.Count;

	/// <summary>
	/// Gets or sets the element at the specified index.
	/// </summary>
	public JsonValue this[int index]
	{
		get => _list[index];
		set => _list[index] = value;
	}

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
		_list.Add(new JsonBoolean(value));
		return this;
	}

	/// <inheritdoc />
	public override int CompareTo(object obj)
	{
		if (obj is JsonArray jObject)
		{
			return Comparer.Compare(_list, jObject._list) ? 0 : 1;
		}

		return -1;
	}

	/// <inheritdoc />
	public IObjectConsumer CompleteObject()
	{
		return this;
	}

	/// <inheritdoc />
	public IEnumerator<JsonValue> GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	/// <inheritdoc />
	public IObjectConsumer Null()
	{
		_list.Add(new JsonNull());
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer Number(object value)
	{
		_list.Add(new JsonNumber(value));
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer StartObject(Type type)
	{
		if (type == typeof(JsonArray))
		{
			var result = new JsonArray();
			_list.Add(result);
			return result;
		}
		else
		{
			var result = new JsonObject();
			_list.Add(result);
			return result;
		}
	}

	/// <inheritdoc />
	public IObjectConsumer String(string value)
	{
		_list.Add(new JsonString(value));
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer WriteObject(object value)
	{
		if (value is JsonValue jValue)
		{
			_list.Add(jValue);
			return this;
		}

		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer WriteProperty(string name, object value)
	{
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer WriteProperty(PropertyInfo info, object value)
	{
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer WriteRawString(string value)
	{
		return this;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	#endregion
}