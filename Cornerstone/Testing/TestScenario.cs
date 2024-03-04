#region References

using System;

#endregion

namespace Cornerstone.Testing;

/// <summary>
/// Represent a test scenario.
/// </summary>
public class TestScenario<T, T2> : TestScenario
{
	#region Constructors

	/// <summary>
	/// Initialize the converter scenario.
	/// </summary>
	/// <param name="name"> The name of the scenario. </param>
	/// <param name="from"> The object to convert from. </param>
	/// <param name="to"> The expected value from the converter. </param>
	public TestScenario(string name, T from, T2 to)
		: base(name, from, typeof(T), to, typeof(T2))
	{
		Name = name;
	}

	#endregion
}

/// <summary>
/// Represent a test scenario.
/// </summary>
public class TestScenario
{
	#region Constructors

	/// <summary>
	/// Initialize the converter scenario.
	/// </summary>
	/// <param name="name"> The name of the scenario. </param>
	/// <param name="from"> The object to convert from. </param>
	/// <param name="to"> The expected value from the converter. </param>
	public TestScenario(string name, object from, object to)
		: this(name, from, from.GetType(), to, to.GetType())
	{
	}

	/// <summary>
	/// Initialize the converter scenario.
	/// </summary>
	/// <param name="name"> The name of the scenario. </param>
	/// <param name="from"> The object to convert from. </param>
	/// <param name="fromType"> The expected type of the <see cref="From" /> value. </param>
	/// <param name="to"> The expected value from the converter. </param>
	/// <param name="toType"> The expected type of the <see cref="To" /> value. </param>
	public TestScenario(string name, object from, Type fromType, object to, Type toType)
	{
		Name = name;
		From = new TestScenarioValue(fromType, from);
		To = new TestScenarioValue(toType, to);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The object to convert from.
	/// </summary>
	public TestScenarioValue From { get; set; }

	/// <summary>
	/// The name of the scenario.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The expected value from the converter.
	/// </summary>
	public TestScenarioValue To { get; set; }

	#endregion
}