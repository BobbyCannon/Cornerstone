#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Compare;

[TestClass]
public class ComparerOptionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Defaults()
	{
		var actual = new ComparerOptions();
		var expected = new Dictionary<string, object>
		{
			{ nameof(ComparerOptions.DoubleTolerance), double.Epsilon },
			{ nameof(ComparerOptions.FloatTolerance), float.Epsilon },
			{ nameof(ComparerOptions.IgnoreObjectTypes), true },
			{ nameof(ComparerOptions.IgnoreMissingProperties), false },
			{ nameof(ComparerOptions.StringComparison), StringComparison.CurrentCulture },
			{ nameof(ComparerOptions.IncludeExcludeOptions), new Dictionary<Type, string[]>() }
		};

		var properties = actual.GetType().GetCachedProperties().OrderBy(x => x.Name).ToList();
		var missingProperties = properties.Select(x => x.Name).Except(expected.Keys).ToList();

		// Ensure we are testing all properties
		IsTrue(missingProperties.Count == 0, $"Missing keys: {string.Join(",", missingProperties)}");

		foreach (var property in properties)
		{
			var expectedValue = expected[property.Name];
			var actualValue = property.GetValue(actual);
			AreEqual(expectedValue, actualValue, $"{property.Name} is incorrect.");
		}
	}

	[TestMethod]
	public void IgnorePropertyType()
	{
		var options = new ComparerOptions();
		options.IgnoreProperty<ComparerTests.Person>(x => x.FullName);

		var actual = options.IncludeExcludeOptions;
		var expected = new Dictionary<Type, IncludeExcludeOptions>
		{
			{ typeof(ComparerTests.Person), new IncludeExcludeOptions(null, ["FullName"]) }
		};

		AreEqual(expected, actual);
	}

	#endregion
}