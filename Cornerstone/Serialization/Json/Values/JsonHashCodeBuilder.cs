#region References

using System;
using System.Reflection;
using Cornerstone.Serialization.Consumer;

#endregion

namespace Cornerstone.Serialization.Json.Values;

internal class JsonHashCodeBuilder : IObjectConsumer
{
	#region Constructors

	public JsonHashCodeBuilder()
	{
		HashCode = 1;
	}

	#endregion

	#region Properties

	public int HashCode { get; private set; }

	/// <summary>
	/// The current mode of the consumer.
	/// </summary>
	public ObjectConsumerMode Mode { get; protected set; }

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
		HashCode = (HashCode * 7) + value.GetHashCode();
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer CompleteObject()
	{
		HashCode += 11;
		return this;
	}

	public IObjectConsumer Null()
	{
		HashCode *= 3;
		return this;
	}

	public IObjectConsumer Number(object value)
	{
		HashCode = (HashCode * 5) + value.GetHashCode();
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer StartObject(Type type)
	{
		var isArray = type.IsArray;
		if (isArray)
		{
			HashCode *= 2;
			Mode = ObjectConsumerMode.Array;
		}
		else
		{
			HashCode += 19;
			Mode = ObjectConsumerMode.Object;
		}
		return this;
	}

	public IObjectConsumer String(string value)
	{
		HashCode = (HashCode * 13) + value.GetHashCode();
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer WriteObject(object value)
	{
		HashCode = (HashCode * 21) + value.GetHashCode();
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer WriteProperty(string name, object value)
	{
		HashCode = (HashCode * 17) + value.GetHashCode();
		return this;
	}

	/// <inheritdoc />
	public IObjectConsumer WriteProperty(PropertyInfo info, object value)
	{
		return WriteProperty(info.Name, value);
	}

	/// <inheritdoc />
	public IObjectConsumer WriteRawString(string value)
	{
		HashCode = (HashCode * 9) + value.GetHashCode();
		return this;
	}

	#endregion
}