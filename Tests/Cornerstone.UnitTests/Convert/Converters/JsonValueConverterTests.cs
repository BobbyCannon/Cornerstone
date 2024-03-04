#region References

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Convert.Converters;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Convert.Converters;

[TestClass]
public class JsonValueConverterTests : ConverterTests<JsonValueConverter>
{
	#region Methods

	[TestMethod]
	public void CanConvert()
	{
		ValidateCanConvert();

		IsTrue(Converter.CanConvert(typeof(JsonObject), typeof(IDictionary)));
		IsTrue(Converter.CanConvert(typeof(JsonObject), typeof(Dictionary<,>)));
	}

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios($"{nameof(JsonValueConverterTests)}.cs",
			EnableFileUpdates || IsDebugging
		);
	}

	[TestMethod]
	public void RunScenarios()
	{
		var scenarios = GetTestScenarios();
		
		foreach (var scenario in scenarios)
		{
			var result = Converter.TryConvertTo(
				scenario.From.Value, scenario.From.Type,
				scenario.To.Type, out var value
			);

			IsTrue(result);
			AreEqual(scenario.To.Value, value);
		}
	}

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	[SuppressMessage("ReSharper", "RedundantCast")]
	private TestScenario[] GetTestScenarios()
	{
		var response = new List<TestScenario>();

		response.AddRange(new TestScenario[]
		{
			// <Scenarios>
			// </Scenarios>
		});

		response.Add(new TestScenario($"{response.Count} Array To Interface",
				new JsonArray(new JsonNumber(1), new JsonNumber(2), new JsonNumber(3), new JsonNumber(4)),
				typeof(JsonArray),
				new List<int> { 1, 2, 3, 4 },
				typeof(IList<int>)
			)
		);

		return response.ToArray();
	}

	#endregion
}