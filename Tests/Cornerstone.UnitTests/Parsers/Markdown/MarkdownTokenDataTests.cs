#region References

using System.Linq;
using Cornerstone.Parsers.Html;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Markdown;

[TestClass]
public class MarkdownTokenDataTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Tokenize()
	{
		var scenarios = new (string input, MarkdownTokenData token)[]
		{
			//   1234 5 6789012345678 9 01 2 34 5 678
			(
				"```\r\nUnknown Code\r\n{\r\n}\r\n```",
				new MarkdownTokenData { ColumnNumber = 1, ElementName = "pre", EndIndex = 28, LineNumber = 1, TokenIndexes = [0, 5, 22, 25], Type = MarkdownTokenType.Code }
			),
			(
				"```CSharp\r\npublic void Test()\r\n{\r\n}\r\n```",
				new MarkdownTokenData { ColumnNumber = 1, ElementName = "pre", EndIndex = 40, LineNumber = 1, TokenIndexes = [0, 3, 8, 11, 34, 37], Type = MarkdownTokenType.Code }
			),
			(
				"[Test](http://test.com)",
				new MarkdownTokenData { ColumnNumber = 1, ElementName = "a", EndIndex = 23, LineNumber = 1, TokenIndexes = [0, 5, 6, 22], Type = MarkdownTokenType.Link }
			)
		};

		foreach (var scenario in scenarios)
		{
			var tokenizer = new MarkdownTokenizer();
			tokenizer.Add(scenario.input);
			var actual = tokenizer.GetTokens().ToArray();
			AreEqual(1, actual.Length);
			AreEqual(scenario.token, actual[0], () => actual[0].DumpCSharp());
		}
	}

	[TestMethod]
	public void TypeOfLink()
	{
		var data = new MarkdownTokenData
		{
			ElementName = "a",
			Type = MarkdownTokenType.Link,
			TokenIndexes = [0, 5, 6, 22],
			StartIndex = 0,
			EndIndex = 22,
			ColumnNumber = 1,
			LineNumber = 1
		};

		//                                1        1         2  23
		//                                01234567890123456789012
		var document = TextDocument.Load("[Test](http://test.com)");
		data.SetDocument(document);

		var writer = new HtmlWriter();
		data.WriteTo(writer);

		AreEqual("<a href=\"http://test.com\">Test</a>", writer.ToString());
	}
	
	[TestMethod]
	public void TypeOfCode()
	{
		var data = new MarkdownTokenData { ColumnNumber = 1, ElementName = "pre", EndIndex = 40, LineNumber = 1, TokenIndexes = [0, 3, 8, 11, 34, 37], Type = MarkdownTokenType.Code };

		//                                1        1           2  23
		//                                0123456789 0 123456789012
		var document = TextDocument.Load("```CSharp\r\npublic void Test()\r\n{\r\n}\r\n```");
		data.SetDocument(document);

		var writer = new HtmlWriter();
		data.WriteTo(writer);

		AreEqual("<pre><span class=\"syntaxKeyword\">public</span> void <span class=\"syntaxIdentifier\">Test</span>()\r\n{\r\n}</pre>", writer.ToString());
	}

	#endregion
}