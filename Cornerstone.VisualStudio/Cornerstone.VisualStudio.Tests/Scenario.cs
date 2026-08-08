#region References

using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

#endregion

namespace Cornerstone.VisualStudio.Tests;

/// <summary>
/// Test Scenario
/// </summary>
/// <param name="Description"> </param>
/// <param name="Expected"> </param>
/// <param name="Agrument"> </param>
public record class Scenario(string Description, object Expected, object Agrument)
{
	#region Methods

	public override string ToString()
	{
		return Description;
	}

	#endregion
}

/// <summary>
/// Provides a data source for a data theory, with the data coming from inline values.
/// </summary>
public sealed class ScenarioAttribute : DataAttribute
{
	#region Fields

	private readonly object[] data;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="ScenarioAttribute" /> class.
	/// </summary>
	/// <param name="description"> The description of test scenario </param>
	/// <param name="expected"> The expected value </param>
	/// <param name="agrument"> The argument of pass to test method. </param>
	public ScenarioAttribute(string description, object expected, object agrument)
	{
		Scenario scenario = new(description, expected, agrument);
		data = [scenario];
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override IEnumerable<object[]> GetData(MethodInfo testMethod)
	{
		yield return data;
	}

	#endregion
}