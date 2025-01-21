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
public class ComparerSettingsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Defaults()
	{
		var actual = new ComparerSettings();
		var expected = new Dictionary<string, object>
		{
			{ nameof(ComparerSettings.DoubleTolerance), double.Epsilon },
			{ nameof(ComparerSettings.FloatTolerance), float.Epsilon },
			{ nameof(ComparerSettings.GlobalIncludeExcludeSettings), IncludeExcludeSettings.Empty },
			{ nameof(ComparerSettings.IgnoreObjectTypes), true },
			{ nameof(ComparerSettings.IgnoreMissingDictionaryEntries), false },
			{ nameof(ComparerSettings.IgnoreMissingProperties), false },
			{ nameof(ComparerSettings.MaxDepth), int.MaxValue },
			{ nameof(ComparerSettings.StringComparison), StringComparison.CurrentCulture },
			{ nameof(ComparerSettings.TypeIncludeExcludeSettings), new Dictionary<Type, string[]>() }
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
		var options = new ComparerSettings();
		options.IgnoreTypeProperty<ComparerTests.Person>(x => x.FullName);

		var actual = options.TypeIncludeExcludeSettings;
		var expected = new Dictionary<Type, IncludeExcludeSettings>
		{
			{ typeof(ComparerTests.Person), new IncludeExcludeSettings(null, ["FullName"]) }
		};

		AreEqual(expected, actual);
	}

	#endregion
}