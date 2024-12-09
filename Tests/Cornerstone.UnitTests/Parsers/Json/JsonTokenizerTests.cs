#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Automation.Web.Browsers;
using Cornerstone.Extensions;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Json;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Json;

[TestClass]
public class JsonTokenizerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void SimpleTest()
	{
		var scenarios = new Dictionary<string, JsonTokenData[]>
		{
			{
				"{}",
				[
					new JsonTokenData { ColumnNumber = 1, EndIndex = 1, IsPropertyName = false, LineNumber = 1, StartIndex = 0, Type = JsonTokenType.CurlyOpen, Value = null },
					new JsonTokenData { ColumnNumber = 2, EndIndex = 2, IsPropertyName = false, LineNumber = 1, StartIndex = 1, Type = JsonTokenType.CurlyClose, Value = null }
				]
			},
			{
				"{\r\n}",
				[
					new JsonTokenData { ColumnNumber = 1, EndIndex = 1, LineNumber = 1, Type = JsonTokenType.CurlyOpen },
					new JsonTokenData { ColumnNumber = 2, EndIndex = 3, LineNumber = 1, StartIndex = 1, Type = JsonTokenType.NewLine },
					new JsonTokenData { ColumnNumber = 1, EndIndex = 4, LineNumber = 2, StartIndex = 3, Type = JsonTokenType.CurlyClose }
				]
			},
			{
				"""
				{
					"foo": 123.45
				}
				""",
				[
					new JsonTokenData { ColumnNumber = 1, EndIndex = 1, LineNumber = 1, Type = JsonTokenType.CurlyOpen },
					new JsonTokenData { ColumnNumber = 2, EndIndex = 3, LineNumber = 1, StartIndex = 1, Type = JsonTokenType.NewLine },
					new JsonTokenData { ColumnNumber = 1, EndIndex = 4, LineNumber = 2, StartIndex = 3, Type = JsonTokenType.Whitespace },
					new JsonTokenData { ColumnNumber = 2, EndIndex = 9, IsPropertyName = true, LineNumber = 2, StartIndex = 4, Type = JsonTokenType.String },
					new JsonTokenData { ColumnNumber = 7, EndIndex = 10, LineNumber = 2, StartIndex = 9, Type = JsonTokenType.Colon },
					new JsonTokenData { ColumnNumber = 8, EndIndex = 11, LineNumber = 2, StartIndex = 10, Type = JsonTokenType.Whitespace },
					new JsonTokenData { ColumnNumber = 9, EndIndex = 17, LineNumber = 2, StartIndex = 11, Type = JsonTokenType.NumberFloat, Value = 123.45 },
					new JsonTokenData { ColumnNumber = 15, EndIndex = 19, LineNumber = 2, StartIndex = 17, Type = JsonTokenType.NewLine },
					new JsonTokenData { ColumnNumber = 1, EndIndex = 20, LineNumber = 3, StartIndex = 19, Type = JsonTokenType.CurlyClose }
				]
			},
			{
				"""
				{
					"Options": ["One", "Two"]
				}
				""",
				[
					new JsonTokenData { ColumnNumber = 1, EndIndex = 1, LineNumber = 1, Type = JsonTokenType.CurlyOpen },
					new JsonTokenData { ColumnNumber = 2, EndIndex = 3, LineNumber = 1, StartIndex = 1, Type = JsonTokenType.NewLine },
					new JsonTokenData { ColumnNumber = 1, EndIndex = 4, LineNumber = 2, StartIndex = 3, Type = JsonTokenType.Whitespace },
					new JsonTokenData { ColumnNumber = 2, EndIndex = 13, IsPropertyName = true, LineNumber = 2, StartIndex = 4, Type = JsonTokenType.String },
					new JsonTokenData { ColumnNumber = 11, EndIndex = 14, LineNumber = 2, StartIndex = 13, Type = JsonTokenType.Colon },
					new JsonTokenData { ColumnNumber = 12, EndIndex = 15, LineNumber = 2, StartIndex = 14, Type = JsonTokenType.Whitespace },
					new JsonTokenData { ColumnNumber = 13, EndIndex = 16, LineNumber = 2, StartIndex = 15, Type = JsonTokenType.SquaredOpen },
					new JsonTokenData { ColumnNumber = 14, EndIndex = 21, LineNumber = 2, StartIndex = 16, Type = JsonTokenType.String },
					new JsonTokenData { ColumnNumber = 19, EndIndex = 22, LineNumber = 2, StartIndex = 21, Type = JsonTokenType.Comma },
					new JsonTokenData { ColumnNumber = 20, EndIndex = 23, LineNumber = 2, StartIndex = 22, Type = JsonTokenType.Whitespace },
					new JsonTokenData { ColumnNumber = 21, EndIndex = 28, LineNumber = 2, StartIndex = 23, Type = JsonTokenType.String },
					new JsonTokenData { ColumnNumber = 26, EndIndex = 29, LineNumber = 2, StartIndex = 28, Type = JsonTokenType.SquaredClose },
					new JsonTokenData { ColumnNumber = 27, EndIndex = 31, LineNumber = 2, StartIndex = 29, Type = JsonTokenType.NewLine },
					new JsonTokenData { ColumnNumber = 1, EndIndex = 32, LineNumber = 3, StartIndex = 31, Type = JsonTokenType.CurlyClose }
				]
			}
		};

		foreach (var scenario in scenarios)
		{
			var t = new JsonTokenizer();
			t.Add(scenario.Key);
			var actual = t.GetTokens().ToArray();
			CopyToClipboard(actual.DumpCSharpArray());
			AreEqual(scenario.Value, actual);
		}
	}

	[TestMethod]
	public void Strings()
	{
		var scenarios = new Dictionary<string, string>
		{
			//          0 123 456 7890 
			//          "\"\\\/"
			{ "\"\\/", "\"\\\"\\\\\\/\"" },
			{ "\f", "\"\\f\"" },
			{ "\n", "\"\\n\"" },
			{ "\n\r", "\"\\n\\r\"" },
			{ "\b\f\n\r\t\u0000", "\"\\b\\f\\n\\r\\t\\u0000\"" },
			{ "\"\\/\b\f\n\r\t\u0000", "\"\\\"\\\\\\/\\b\\f\\n\\r\\t\\u0000\"" },
			{ "\uFFFF", "\"\\uFFFF\"" },
			{ "'''\"'''", "\"'''\\\"'''\"" }
		};

		var index = 0;
		foreach (var scenario in scenarios)
		{
			index++.Dump();

			var actual = scenario.Key.ToJson();
			AreEqual(scenario.Value, actual, () => actual);

			var tokenizer = new JsonTokenizer();
			tokenizer.Add(actual);
			tokenizer.ParseNext();

			var escapedString = tokenizer.CurrentToken.ToString();
			AreEqual(scenario.Value, escapedString);

			var unescapedString = JsonString.Unescape(tokenizer.CurrentToken);
			AreEqual(scenario.Key, unescapedString);
		}
	}

	[TestMethod]
	public void ToCodeSyntaxHtml()
	{
		var json = GetContentToTest();
		var actual = Tokenizer.ToCodeSyntaxHtml<JsonTokenizer>(json, true);
		actual.Dump();

		if (EnableBrowserSamples)
		{
			using var browser = Chrome.AttachOrCreate();
			browser.AutoClose = false;
			browser.SetHtml(actual);
		}
	}

	private string GetContentToTest()
	{
		return """
				{
					"Foo": "bar",
					"Enabled": true,
					"Age": 21,
					"Weight": 155.12345,
					"ModifiedOn": "2024-09-27T15:04:35.4587703Z",
					"Data": [1, 2, 3, 4, 5],
					"Account": {
						"FirstName": "John"
					},
					"AvailableOptions": ["One","Two","Three"]
				}
				""";
	}

	#endregion
}