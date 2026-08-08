#region References

using System;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Markdown;

[TestClass]
public class MarkdownRendererTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ExtractCodeBlockInfo()
	{
		Process("""
				``` csharp
				public void Test()
				{
					System.Console.WriteLine("Test");
				}
				```
				""", "csharp", 12, 60);
		Process("""
				~~~ csharp
				public void Test()
				{
					System.Console.WriteLine("Test");
				}
				~~~
				""", "csharp", 12, 60);
	}

	[TestMethod]
	public void ExtractCodeBlockInfoUntaggedFenceWithTreeBody()
	{
		// Untagged ``` often leaves ContentRegionStart on the opening newline; body must still extract.
		const string markdown =
			"""
			```
			AppKeystone
			    ├── AppState
			    ├── AppBus
			    |   └── Channels
			    └── AppEngine
			```
			""";

		var buffer = new StringBuffer(markdown);
		var parser = new MarkdownParser(buffer, new SpeedyQueue<Block>());
		var blocks = parser.Process().ToArray();
		var code = blocks.First(b => b.Type == MarkdownTokenizer.TokenTypeCodeBlock);
		var actual = MarkdownRenderer.ExtractCodeBlockInfo(markdown.AsSpan(), code);

		AreEqual(string.Empty, actual.language);
		IsTrue(actual.contentLength > 0, () => $"Expected body content, got length {actual.contentLength}");
		var body = markdown.AsSpan(actual.contentStartOffset, actual.contentLength).ToString();
		IsTrue(body.Contains("AppKeystone"), () => body);
		IsTrue(body.Contains("AppState"), () => body);
		IsTrue(body.Contains("Channels"), () => body);
	}

	private static void Process(string code, string language, int start, int length)
	{
		var buffer = new StringBuffer(code);
		var parser = new MarkdownParser(buffer, new SpeedyQueue<Block>());
		var blocks = parser.Process().ToArray();
		var actual = MarkdownRenderer.ExtractCodeBlockInfo(code, blocks[0]);
		AreEqual(language, actual.language);
		AreEqual(start, actual.contentStartOffset);
		AreEqual(length, actual.contentLength);
	}

	#endregion
}