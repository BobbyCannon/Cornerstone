#region References

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ScenarioAttribute : Attribute, ITestDataSource
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

	public IEnumerable<object[]> GetData(MethodInfo methodInfo)
	{
		yield return data;
	}

	public string GetDisplayName(MethodInfo methodInfo, object[] data)
	{
		if ((data != null) && (data.Length > 0) && (data[0] is Scenario scenario))
		{
			return scenario.Description;
		}

		return methodInfo.Name;
	}

	#endregion
}