#region References

using System;

#endregion

namespace Cornerstone.Testing;

/// <inheritdoc />
public class SerializationScenario<T> : SerializationScenario
{
	#region Constructors

	/// <summary>
	/// Initialize a serialization scenario with a set of values.
	/// </summary>
	/// <param name="name"> The name of the scenario. </param>
	/// <param name="value"> The value to be serialized. </param>
	/// <param name="expected"> The expected output in text format. </param>
	public SerializationScenario(string name, T value, string expected)
		: base(name, value, typeof(T), expected)
	{
	}

	#endregion
}

/// <summary>
/// Represents a scenario for serialization
/// </summary>
public class SerializationScenario
{
	#region Constructors

	/// <summary>
	/// Initialize the serialization scenario.
	/// </summary>
	/// <param name="name"> The scenario name. </param>
	/// <param name="value"> The value. </param>
	/// <param name="type"> The value type. </param>
	/// <param name="expected"> The expected string. </param>
	public SerializationScenario(string name, object value, Type type, string expected)
	{
		Expected = expected;
		Name = name;
		Type = type;
		Value = value;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The expected output in text format.
	/// </summary>
	public string Expected { get; set; }

	/// <summary>
	/// The name of the scenario.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The type of the <see cref="Value" />.
	/// </summary>
	public Type Type { get; set; }

	/// <summary>
	/// The value to be serialized.
	/// </summary>
	public object Value { get; set; }

	#endregion
}