#region References

using System;
using System.Linq;
using Cornerstone.Compare;
using Cornerstone.Serialization.Consumer;

#endregion

namespace Cornerstone.Serialization.Json.Values;

/// <summary>
/// This class represents a JSON number.
/// </summary>
public class JsonNumber : JsonValue
{
	#region Constructors

	/// <summary>
	/// Initializes a JSON number with an number type.
	/// </summary>
	/// <param name="value"> The number value. </param>
	public JsonNumber(object value)
	{
		if ((value == null) || !Activator.NumberTypes.Contains(value.GetType()))
		{
			throw new ArgumentException(Babel.Tower[BabelKeys.ArgumentInvalid], nameof(value));
		}

		Value = value;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The underlying value. This can be either be a double, a long or an ulong.
	/// </summary>
	public object Value { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override int CompareTo(object obj)
	{
		var objValue = obj is JsonNumber jsonValue ? jsonValue.Value : obj;
		var session = Comparer.StartSession(Value, objValue);
		session.Compare();
		return session.Result == CompareResult.AreEqual ? 0 : -1;
	}

	/// <inheritdoc />
	public override void WriteTo(IObjectConsumer consumer)
	{
		consumer.Number(Value);
	}

	#endregion
}