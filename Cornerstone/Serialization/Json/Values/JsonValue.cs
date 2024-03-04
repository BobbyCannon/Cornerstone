#region References

using System;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Consumers;

#endregion

namespace Cornerstone.Serialization.Json.Values;

/// <summary>
/// The base class for the object model of
/// arbitrary JSON data.
/// </summary>
public abstract class JsonValue : IComparable
{
	#region Constructors

	/// <summary>
	/// Initialize a JSON value.
	/// </summary>
	protected JsonValue()
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public abstract int CompareTo(object obj);

	/// <summary>
	/// Compares this JSON value to the given object for equality.
	/// </summary>
	/// <param name="obj">
	/// The other JSON value. If this is null or does not
	/// inherit <see cref="JsonValue" /> false is returned.
	/// </param>
	/// <returns> true, if the objects are equal; false, otherwise. </returns>
	public override bool Equals(object obj)
	{
		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		if (!(obj is JsonValue jsonValue))
		{
			return false;
		}

		// we use the string representation for equality comparison
		return jsonValue.CompareTo(obj) == 0;
	}

	/// <summary>
	/// Computes a hash code for this JSON value.
	/// </summary>
	/// <returns> The hash code for this JSON value. </returns>
	public override int GetHashCode()
	{
		var hashCodeBuilder = new JsonHashCodeBuilder();
		WriteTo(hashCodeBuilder);
		return hashCodeBuilder.HashCode;
	}

	/// <summary>
	/// Converts this generic object model to a JSON string.
	/// </summary>
	/// <returns> The JSON string. </returns>
	public override string ToString()
	{
		var writer = new TextJsonConsumer();
		WriteTo(writer);
		return writer.ToString();
	}

	/// <summary>
	/// Writes this <see cref="JsonValue" /> to a <see cref="IObjectConsumer" />.
	/// </summary>
	/// <param name="consumer"> The consumer. </param>
	public virtual void WriteTo(IObjectConsumer consumer)
	{
		if (consumer is JsonHashCodeBuilder)
		{
			return;
		}

		consumer.WriteObject(this);
	}

	#endregion
}