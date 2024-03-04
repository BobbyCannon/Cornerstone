#region References

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Parsers.Json;
using Cornerstone.Serialization.Json;
using Cornerstone.Serialization.Json.Values;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json;

[TestClass]
public class TokenizerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ConvertWithNestedQuotes()
	{
		var dictionary = new Dictionary<string, string>
		{
			{
				"a",
				new Dictionary<string, string>
				{
					{
						"b",
						new Dictionary<string, string>
						{
							{ "c", "d" }
						}.ToJson()
					}
				}.ToJson()
			}
		};

		var json = dictionary.ToJson();
		var scenarios = new[] { '\"', '\'' };

		foreach (var scenario in scenarios)
		{
			var scenarioJson = json.Replace('\"', scenario);
			var jsonObject = (JsonObject) JsonSerializer.Parse(scenarioJson);
			AreEqual(1, jsonObject.Keys.Count());

			var actual = (JsonString) jsonObject["a"];
			var expected = "{\"b\":\"{\\\"c\\\":\\\"d\\\"}\"}".Replace('\"', scenario);
			AreEqual(expected, actual.Value, () => actual.Value);

			jsonObject = (JsonObject) JsonSerializer.Parse(actual.Value);
			actual = (JsonString) jsonObject["b"];
			expected = "{\"c\":\"d\"}".Replace('\"', scenario);
			AreEqual(expected, actual.Value, () => actual.Value);
		}
	}

	[TestMethod]
	public void Strings()
	{
		var scenarios = new Dictionary<string, string>
		{
			{ "\uFFFF", "\"\\uFFFF\"" },
			{ "\"\\/\b\f\n\r\t\u0000", "\"\\\"\\\\\\/\\b\\f\\n\\r\\t\\u0000\"" },
			{ "'''\"'''", "\"'''\\\"'''\"" },
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.Key.ToJson();
			AreEqual(scenario.Value, actual, () => actual);

			var service = new JsonTokenizer(new StringReader(actual));
			service.MoveNext();

			AreEqual(scenario.Key, service.CurrentToken.StringValue);
		}
	}

	#endregion
}