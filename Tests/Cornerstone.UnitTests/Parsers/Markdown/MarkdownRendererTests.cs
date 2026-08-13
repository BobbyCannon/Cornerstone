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

	[TestMethod]
	public void ExtractCodeBlockInfoStaleOffsetsPastBufferReturnEmpty()
	{
		// Streaming: UI can still hold a code block whose Offsets point past a shorter buffer.
		const string markdown =
			"""
			```csharp
			var x = 1;
			```
			""";

		var buffer = new StringBuffer(markdown);
		var parser = new MarkdownParser(buffer, new SpeedyQueue<Block>());
		var blocks = parser.Process().ToArray();
		var code = blocks.First(b => b.Type == MarkdownTokenizer.TokenTypeCodeBlock);

		// Truncated buffer (mid-stream shrink / partial view) with unmoved offsets.
		var shortBuffer = markdown.AsSpan(0, Math.Min(8, markdown.Length));
		var actual = MarkdownRenderer.ExtractCodeBlockInfo(shortBuffer, code);

		// Must not throw; empty/clamped body is fine.
		IsTrue(actual.contentLength >= 0);
		IsTrue(actual.contentStartOffset >= 0);
		IsTrue((actual.contentStartOffset + actual.contentLength) <= shortBuffer.Length);
	}

	[TestMethod]
	public void ExtractCodeBlockInfoOffsetsBeyondEofReturnEmpty()
	{
		var block = new Block
		{
			Type = MarkdownTokenizer.TokenTypeCodeBlock,
			StartOffset = 0,
			EndOffset = 5,
			Offsets = [100, 200]
		};

		var actual = MarkdownRenderer.ExtractCodeBlockInfo("short".AsSpan(), block);
		AreEqual(string.Empty, actual.language);
		AreEqual(0, actual.contentLength);
	}

	[TestMethod]
	public void ExtractHeaderInfo()
	{
		const string markdown = "# Header";
		var buffer = new StringBuffer(markdown);
		var parser = new MarkdownParser(buffer, new SpeedyQueue<Block>());
		var blocks = parser.Process().ToArray();
		var actual = MarkdownRenderer.ExtractHeaderInfo(markdown.AsSpan(), blocks[0]);

		AreEqual(1, actual.size);
		AreEqual(2, actual.contentStartOffset);
		AreEqual(6, actual.contentLength);
		AreEqual("Header", markdown.AsSpan(actual.contentStartOffset, actual.contentLength).ToString());
	}

	[TestMethod]
	public void ExtractHeaderInfoStaleOffsetsPastBufferReturnEmpty()
	{
		// Streaming: UI can still hold a header whose Offsets/EndOffset point past a shorter buffer.
		const string markdown = "## Title here";
		var buffer = new StringBuffer(markdown);
		var parser = new MarkdownParser(buffer, new SpeedyQueue<Block>());
		var blocks = parser.Process().ToArray();
		var header = blocks.First(b => b.Type == MarkdownTokenizer.TokenTypeHeader);

		var shortBuffer = markdown.AsSpan(0, Math.Min(3, markdown.Length));
		var actual = MarkdownRenderer.ExtractHeaderInfo(shortBuffer, header);

		IsTrue(actual.size >= 1);
		IsTrue(actual.contentLength >= 0);
		IsTrue(actual.contentStartOffset >= 0);
		IsTrue((actual.contentStartOffset + actual.contentLength) <= shortBuffer.Length);
	}

	[TestMethod]
	public void ExtractHeaderInfoOffsetsBeyondEofReturnEmpty()
	{
		var block = new Block
		{
			Type = MarkdownTokenizer.TokenTypeHeader,
			StartOffset = 0,
			EndOffset = 50,
			Offsets = [3, 100]
		};

		var actual = MarkdownRenderer.ExtractHeaderInfo("short".AsSpan(), block);
		AreEqual(3, actual.size);
		AreEqual(0, actual.contentLength);
		IsTrue((actual.contentStartOffset + actual.contentLength) <= "short".Length);
	}

	[TestMethod]
	public void SafeSliceClampsStaleBlockRanges()
	{
		const string text = "hello";
		var span = text.AsSpan();

		AreEqual("ell", MarkdownRenderer.SafeSlice(span, 1, 3).ToString());
		AreEqual("lo", MarkdownRenderer.SafeSlice(span, 3, 99).ToString());
		IsTrue(MarkdownRenderer.SafeSlice(span, 50, 10).IsEmpty);
		IsTrue(MarkdownRenderer.SafeSlice(span, 0, 0).IsEmpty);
		// start < 0: length is reduced by |start|, start becomes 0 → "h"
		AreEqual("h", MarkdownRenderer.SafeSlice(span, -2, 3).ToString());

		var block = new Block { StartOffset = 100, EndOffset = 120 };
		IsTrue(MarkdownRenderer.SafeSlice(span, block).IsEmpty);

		block = new Block { StartOffset = 2, EndOffset = 100 };
		AreEqual("llo", MarkdownRenderer.SafeSlice(span, block).ToString());
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