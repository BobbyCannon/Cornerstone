#region References

using System.Collections.Generic;
using Cornerstone.Serialization;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json.Converters;

[TestClass]
public class DictionaryConverterTests : JsonConverterTest<DictionaryConverter>
{
	#region Methods

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios(
			$"{nameof(DictionaryConverterTests)}.cs",
			EnableFileUpdates || IsDebugging,
			new Dictionary<string, int> { { "One", 1 }, { "Two", 2 } }
		);
	}

	[TestMethod]
	public void Indented()
	{
		var value = new Dictionary<string, int>
		{
			{ "One", 1 },
			{ "Two", 2 }
		};
		var settings = new SerializationOptions
		{
			NamingConvention = NamingConvention.PascalCase,
			TextFormat = TextFormat.Indented
		};
		var expected = "{\r\n\t\"One\": 1,\r\n\t\"Two\": 2\r\n}";
		AreEqual(expected, Converter.GetJsonString(value, settings));
	}

	[TestMethod]
	public void RunScenarios()
	{
		var scenarios = GetScenarios();
		var settings = Serializer.DefaultSettings;

		foreach (var scenario in scenarios)
		{
			var actual = Converter.GetJsonString(scenario.Value, settings);
			AreEqual(scenario.Expected, actual, scenario.Name);
		}
	}

	[TestMethod]
	public void RunSingleScenario()
	{
		var scenario = GetScenarios()[0];
		var settings = Serializer.DefaultSettings;
		var actual = Converter.GetJsonString(scenario.Value, settings);
		AreEqual(scenario.Expected, actual, scenario.Name);
	}

	private SerializationScenario[] GetScenarios()
	{
		var scenarios = new SerializationScenario[]
		{
			// <Scenarios>
			new("0 Dictionary<string, int>", new Dictionary<string, int>
			{
				{ "One", 1 },
				{ "Two", 2 }
			}, typeof(Dictionary<string, int>), "{\"One\":1,\"Two\":2}"),
			// </Scenarios>
		};

		return scenarios;
	}

	#endregion
}