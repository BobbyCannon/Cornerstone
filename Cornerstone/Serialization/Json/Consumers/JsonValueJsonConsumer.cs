#region References

using System;
using System.Reflection;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Consumers;

/// <summary>
/// <see cref="IObjectConsumer" /> implementation which consumes
/// JSON data from any producer and creates a JSON object model,
/// <see cref="JsonValue" /> and derived classes.
/// </summary>
internal sealed class JsonValueJsonConsumer : IObjectConsumer
{
	#region Fields

	public JsonValue Result;

	#endregion

	#region Methods

	/// <inheritdoc />
	public bool AlreadyProcessed(object value)
	{
		return false;
	}

	public IObjectConsumer Boolean(bool value)
	{
		Result = new JsonBoolean(value);
		return this;
	}

	public IObjectConsumer CompleteArrayOrObject()
	{
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer CompleteObject()
	{
		return this;
	}

	public IObjectConsumer Null()
	{
		Result = new JsonNull();
		return this;
	}

	public IObjectConsumer Number(object value)
	{
		Result = new JsonNumber(value);
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer StartObject(Type type)
	{
		if (type.IsArray || (type == typeof(JsonArray)))
		{
			var result = new JsonArray();
			Result = result;
			return result;
		}
		if (type == typeof(JsonObject))
		{
			var result = new JsonObject();
			Result = result;
			return result;
		}
		return this;
	}

	public IObjectConsumer String(string value)
	{
		Result = new JsonString(value);
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer WriteObject(object value)
	{
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

	#endregion
}