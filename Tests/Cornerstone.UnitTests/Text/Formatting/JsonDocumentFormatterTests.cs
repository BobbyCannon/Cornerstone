#region References

using Cornerstone.Collections;
using Cornerstone.Text;
using Cornerstone.Text.Formatting;
using Cornerstone.Text.Parsing;
using Cornerstone.Text.Parsing.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text.Formatting;

[TestClass]
public class JsonDocumentFormatterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CompactArrayPrettyPrints()
	{
		AreEqual("[\n\t1,\n\t2\n]", Format("[1,2]", new JsonFormatOptions { NewLine = "\n" }));
	}

	[TestMethod]
	public void CompactObjectPrettyPrints()
	{
		AreEqual("{\n\t\"a\": 1\n}", Format("{\"a\":1}", new JsonFormatOptions { NewLine = "\n" }));
	}

	[TestMethod]
	public void DetectsWindowsNewLineFromDocument()
	{
		var formatted = Format("{\r\n\"a\":1}", new JsonFormatOptions());
		IsTrue(formatted.Contains("\r\n"));
	}

	[TestMethod]
	public void EmptyContainersStayCompact()
	{
		AreEqual("{}", Format("{}", new JsonFormatOptions { NewLine = "\n" }));
		AreEqual("[]", Format("[]", new JsonFormatOptions { NewLine = "\n" }));
		AreEqual("{\n\t\"a\": {}\n}", Format("{\"a\":{}}", new JsonFormatOptions { NewLine = "\n" }));
	}

	[TestMethod]
	public void InvalidJsonStillReturnsString()
	{
		var formatted = Format("{ foo", new JsonFormatOptions { NewLine = "\n" });
		IsFalse(string.IsNullOrEmpty(formatted));
	}

	[TestMethod]
	public void MinifyRemovesWhitespace()
	{
		AreEqual("{\"a\":1,\"b\":[true,null]}", Format("{\n\t\"a\": 1,\n\t\"b\": [\n\t\ttrue,\n\t\tnull\n\t]\n}", new JsonFormatOptions
		{
			Minify = true,
			NewLine = "\n"
		}));
	}

	[TestMethod]
	public void MixedWhitespaceNormalizes()
	{
		AreEqual("{\n\t\"a\": 1\n}", Format("{\r\n  \"a\" :\t1\r\n}", new JsonFormatOptions { NewLine = "\n" }));
	}

	[TestMethod]
	public void NestedObjectAndArray()
	{
		var expected = "{\n\t\"a\": {\n\t\t\"b\": [\n\t\t\t1,\n\t\t\t2\n\t\t]\n\t}\n}";
		AreEqual(expected, Format("{\"a\":{\"b\":[1,2]}}", new JsonFormatOptions { NewLine = "\n" }));
	}

	[TestMethod]
	public void PrettyPrintIsIdempotent()
	{
		var options = new JsonFormatOptions { NewLine = "\n" };
		var first = Format("{\"a\":1,\"b\":[true,null]}", options);
		AreEqual(first, Format(first, options));
	}

	private string Format(string json, JsonFormatOptions options)
	{
		var buffer = new StringGapBuffer(json);
		var tokenizer = new JsonTokenizer(buffer, new SpeedyQueue<Token>());
		IsTrue(DocumentFormatter.TryFormat(tokenizer, buffer, options, out var formatted));
		return formatted;
	}

	#endregion
}