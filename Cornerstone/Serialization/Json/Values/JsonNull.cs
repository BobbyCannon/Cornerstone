#region References

using System;
using Cornerstone.Serialization.Consumer;

#endregion

namespace Cornerstone.Serialization.Json.Values;

/// <summary>
/// This class represents the JSON null value.
/// </summary>
public class JsonNull : JsonValue
{
	#region Constants

	/// <summary>
	/// The value for the JsonNull value.
	/// </summary>
	public const string Value = "null";

	#endregion

	#region Methods

	/// <inheritdoc />
	public override int CompareTo(object obj)
	{
		return IsNullValue(obj) ? 0 : -1;
	}

	/// <summary>
	/// Determine if the value is a null values from a JSON perspective.
	/// </summary>
	/// <param name="value"> The value to test. </param>
	/// <returns> True if the value is a null type otherwise false. </returns>
	public static bool IsNullValue(object value)
	{
		return value is null
			or DBNull
			or JsonNull;
	}

	/// <inheritdoc />
	public override void WriteTo(IObjectConsumer consumer)
	{
		consumer.Null();
	}

	#endregion
}