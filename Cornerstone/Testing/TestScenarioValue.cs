#region References

using System;

#endregion

namespace Cornerstone.Testing;

/// <summary>
/// Represents a test scenario value.
/// </summary>
public class TestScenarioValue
{
	#region Constructors

	/// <summary>
	/// Initialize a value for a test.
	/// </summary>
	/// <param name="type"> The expected type of the value. </param>
	/// <param name="value"> The value of the type. </param>
	public TestScenarioValue(Type type, object value)
	{
		Value = value;
		Type = type;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The expected type of the <see cref="Value" />.
	/// </summary>
	public Type Type { get; set; }

	/// <summary>
	/// The value of the type.
	/// </summary>
	public object Value { get; set; }

	#endregion
}