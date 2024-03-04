#region References

using Cornerstone.Serialization.Consumer;

#endregion

namespace Cornerstone.Serialization.Json.Values;

/// <summary>
/// This class represents a JSON boolean value.
/// </summary>
public class JsonBoolean : JsonValue
{
	#region Constructors

	/// <summary>
	/// Initializes a new JSON boolean value with the given value.
	/// </summary>
	/// <param name="value"> The boolean value. </param>
	public JsonBoolean(bool value)
	{
		Value = value;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The boolean value of this JSON boolean.
	/// </summary>
	public bool Value { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override int CompareTo(object obj)
	{
		return obj is JsonBoolean jsonValue ? Value.CompareTo(jsonValue.Value) : -1;
	}

	/// <inheritdoc />
	public override void WriteTo(IObjectConsumer consumer)
	{
		consumer.Boolean(Value);
	}

	#endregion
}