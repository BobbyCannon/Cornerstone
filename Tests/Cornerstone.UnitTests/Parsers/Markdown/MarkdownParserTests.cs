#region References

using System;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Markdown;

[TestClass]
public class MarkdownParserTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ParseBlockQuotes()
	{
		Process("> Block Quote", new Block { EndOffset = 13, Type = MarkdownTokenizer.TokenTypeBlockQuote });
		Process("	> Block Quote",
			new Block { EndOffset = 1, Type = TextProcessor.TokenTypeWhitespace },
			new Block { EndOffset = 14, StartOffset = 1, Type = MarkdownTokenizer.TokenTypeBlockQuote }
		);
		Process("""
				> Quotes should be able
				> to go many lines
				> even more lines
				""",
			new Block { EndOffset = 62, Type = MarkdownTokenizer.TokenTypeBlockQuote }
		);
	}

	[TestMethod]
	public void ParseHorizontalRule()
	{
		Process("---", new Block { EndOffset = 3, Type = MarkdownTokenizer.TokenTypeHorizontalRule });
		Process("***", new Block { EndOffset = 3, Type = MarkdownTokenizer.TokenTypeHorizontalRule });
		Process("___", new Block { EndOffset = 3, Type = MarkdownTokenizer.TokenTypeHorizontalRule });
		Process("----", new Block { EndOffset = 4, Type = MarkdownTokenizer.TokenTypeHorizontalRule });
		// Not a rule: list marker or short run
		Process("- item", new Block { EndOffset = 6, Type = MarkdownTokenizer.TokenTypeUnorderedList });
		Process("--", new Block { EndOffset = 2, Type = TextProcessor.TokenTypeText });
	}

	[TestMethod]
	public void ParseInlineCode()
	{
		// Offsets = content region (excludes backticks) for MarkdownInlineProjector
		Process("`code`", new Block { EndOffset = 6, Offsets = [1, 5], Type = MarkdownTokenizer.TokenTypeInlineCode });
		Process("a `x` b",
			new Block { EndOffset = 2, Type = TextProcessor.TokenTypeText },
			new Block { EndOffset = 5, StartOffset = 2, Offsets = [3, 4], Type = MarkdownTokenizer.TokenTypeInlineCode },
			new Block { EndOffset = 6, StartOffset = 5, Type = TextProcessor.TokenTypeWhitespace },
			new Block { EndOffset = 7, StartOffset = 6, Type = TextProcessor.TokenTypeText }
		);
		Process("Every `AppKeystone` exposes",
			new Block { EndOffset = 6, Type = TextProcessor.TokenTypeText },
			new Block { EndOffset = 19, StartOffset = 6, Offsets = [7, 18], Type = MarkdownTokenizer.TokenTypeInlineCode },
			new Block { EndOffset = 20, StartOffset = 19, Type = TextProcessor.TokenTypeWhitespace },
			new Block { EndOffset = 27, StartOffset = 20, Type = TextProcessor.TokenTypeText }
		);
	}

	[TestMethod]
	public void ParseBoldAndItalic()
	{
		// Emphasis expands to interior leaves with Em* flags (parser-owned nesting).
		Process("*Italic*", new Block { EndOffset = 7, StartOffset = 1, Type = TextProcessor.TokenTypeText, EmItalic = true });
		Process("_Italic_", new Block { EndOffset = 7, StartOffset = 1, Type = TextProcessor.TokenTypeText, EmItalic = true });
		Process("**Bold**", new Block { EndOffset = 6, StartOffset = 2, Type = TextProcessor.TokenTypeText, EmBold = true });
		Process("__Bold__", new Block { EndOffset = 6, StartOffset = 2, Type = TextProcessor.TokenTypeText, EmBold = true });
		Process("***BoldAndItalic***", new Block { EndOffset = 16, StartOffset = 3, Type = TextProcessor.TokenTypeText, EmBold = true, EmItalic = true });
		Process("___BoldAndItalic___", new Block { EndOffset = 16, StartOffset = 3, Type = TextProcessor.TokenTypeText, EmBold = true, EmItalic = true });
		Process("~~StrikeThrough~~", new Block { EndOffset = 15, StartOffset = 2, Type = TextProcessor.TokenTypeText, EmStrikethrough = true });
	}

	[TestMethod]
	public void ParseBoldWithSpacesAndOperators()
	{
		// Spaces and + must stay inside one bold span (common agent/math answers).
		Process("**2 + 2 = 4**", new Block { EndOffset = 11, StartOffset = 2, Type = TextProcessor.TokenTypeText, EmBold = true });
		Process("**Bold with spaces**", new Block { EndOffset = 18, StartOffset = 2, Type = TextProcessor.TokenTypeText, EmBold = true });
	}

	[TestMethod]
	public void ParseBoldWrappedLink()
	{
		// **[Agent/README.md](Agent/README.md)** → Link with EmBold (not a single Bold container)
		Process("**[Agent/README.md](Agent/README.md)**",
			new Block
			{
				StartOffset = 2,
				EndOffset = 36,
				Offsets = [3, 18, 20, 35],
				Type = MarkdownTokenizer.TokenTypeLink,
				EmBold = true
			});
	}

	[TestMethod]
	public void ParseLinks()
	{
		// [text](url) → offsets [textStart, textEnd, destStart, destEnd]
		// "[Hi](a.md)" length 10: text 1-3, dest 5-9
		Process("[Hi](a.md)", new Block
		{
			EndOffset = 10,
			Offsets = [1, 3, 5, 9],
			StartOffset = 0,
			Type = MarkdownTokenizer.TokenTypeLink
		});
		Process("See [Keystone](Keystone.md#bus--state--engine) now",
			new Block { EndOffset = 4, Type = TextProcessor.TokenTypeText },
			new Block
			{
				EndOffset = 46,
				Offsets = [5, 13, 15, 45],
				StartOffset = 4,
				Type = MarkdownTokenizer.TokenTypeLink
			},
			new Block { EndOffset = 47, StartOffset = 46, Type = TextProcessor.TokenTypeWhitespace },
			new Block { EndOffset = 50, StartOffset = 47, Type = TextProcessor.TokenTypeText }
		);
		// Incomplete link stays text
		Process("[no close", new Block { EndOffset = 9, Type = TextProcessor.TokenTypeText });
	}

	[TestMethod]
	public void ParseCodeBlock()
	{
		// Mid-line ``` must not close a fence; only a line that is a closing fence closes it.
		Process("""
				``` User Request
				This should be inside the existing code block ```csharp. This should not be split
				```
				""",
			new Block { EndOffset = 104, Offsets = [3, 101], Type = MarkdownTokenizer.TokenTypeCodeBlock }
		);
	}

	[TestMethod]
	public void ParseCodeBlocks()
	{
		Process("""
				```csharp
				public void Test()
				{
					System.Console.WriteLine("Test");
				}
				```
				""",
			new Block { EndOffset = 76, Offsets = [3, 73], Type = MarkdownTokenizer.TokenTypeCodeBlock }
		);
		Process("""
					```csharp
					public void Test()
					{
						System.Console.WriteLine("Test");
					}
					```
				""",
			new Block { EndOffset = 1, Type = TextProcessor.TokenTypeWhitespace },
			new Block { EndOffset = 82, Offsets = [4, 79], StartOffset = 1, Type = MarkdownTokenizer.TokenTypeCodeBlock }
		);
	}

	[TestMethod]
	public void ParseIncompleteCodeBlock()
	{
		// Streaming: open fence with no closer yet is still a CodeBlock through EOF.
		Process("```csharp\npublic void Foo()",
			new Block { EndOffset = 27, Offsets = [3, 27], Type = MarkdownTokenizer.TokenTypeCodeBlock }
		);

		Process("```\npartial",
			new Block { EndOffset = 11, Offsets = [3, 11], Type = MarkdownTokenizer.TokenTypeCodeBlock }
		);

		Process("~~~\nxml-ish",
			new Block { EndOffset = 11, Offsets = [3, 11], Type = MarkdownTokenizer.TokenTypeCodeBlock }
		);
	}

	/// <summary>
	/// Simulates an LLM streaming one character at a time. A fenced code block must become
	/// TokenTypeCodeBlock as soon as the opening fence is complete (3 ticks), stay a code block
	/// while content and a partial closer stream in, and remain a single code block when closed.
	/// </summary>
	[TestMethod]
	public void ParseCodeBlockCharacterByCharacterStream()
	{
		const string markdown =
			"""
			```csharp
			var x = 1;
			```
			done
			""";

		var buffer = new StringGapBuffer();
		var pool = new SpeedyQueue<Block>();
		var parser = new MarkdownParser(buffer, pool);

		var sawCodeBlockWhileOpen = false;
		var sawCodeBlockWhileContent = false;
		var sawCompleteThenTrailingText = false;

		for (var i = 0; i < markdown.Length; i++)
		{
			buffer.Append(markdown[i]);
			var blocks = parser.Process().ToArray();
			var prefix = markdown[..(i + 1)];
			var hasCodeBlock = blocks.Any(b => b.Type == MarkdownTokenizer.TokenTypeCodeBlock);
			var fenceOpenLength = CountLeadingFenceLength(prefix);

			if (fenceOpenLength < 3)
			{
				// 1–2 backticks are not a fence yet; must not claim a code block.
				IsFalse(hasCodeBlock,
					() => $"Unexpected CodeBlock after {i + 1} char(s): {Escape(prefix)}");
				continue;
			}

			// From the 3rd backtick through the end of the document, a code block must exist.
			IsTrue(hasCodeBlock,
				() => $"Expected CodeBlock after {i + 1} char(s): {Escape(prefix)}\nBlocks: {Describe(blocks)}");

			var code = blocks.First(b => b.Type == MarkdownTokenizer.TokenTypeCodeBlock);
			AreEqual(0, code.StartOffset,
				() => $"Code block should start at document start while streaming: {Escape(prefix)}");

			// Incomplete: block should run to EOF until the closing fence line is finished.
			var closed = IsClosedFence(prefix);
			if (!closed)
			{
				AreEqual(buffer.Count, code.EndOffset,
					() => $"Open fence should span to EOF: {Escape(prefix)}");
				sawCodeBlockWhileOpen = true;

				// Once the info string is complete (opening line has a newline), language is known
				// even if the body is still empty (just ```csharp\n).
				var firstLineBreak = prefix.IndexOfAny(['\r', '\n']);
				if (firstLineBreak > 3)
				{
					var info = MarkdownRenderer.ExtractCodeBlockInfo(prefix.AsSpan(), code);
					AreEqual("csharp", info.language,
						() => $"Language should be known mid-stream: {Escape(prefix)}");

					// Body content only once real characters exist after the opening fence line.
					if (HasCodeBodyContent(prefix))
					{
						sawCodeBlockWhileContent = true;
						IsTrue(info.contentLength > 0,
							() => $"Expected content while streaming body: {Escape(prefix)}");
					}
				}
			}
			else
			{
				// Once closed, end is at/after the closer, not necessarily full buffer if trailing text follows.
				IsTrue(code.EndOffset <= buffer.Count);
				IsTrue(code.EndOffset >= "```csharp\nvar x = 1;\n```".Replace("\r\n", "\n").Length - 2,
					() => $"Closed code block end too small: {code.EndOffset}, text={Escape(prefix)}");

				if (prefix.Contains("done", StringComparison.Ordinal))
				{
					// Trailing prose after the fence should not be swallowed by the code block.
					IsTrue(blocks.Any(b =>
							(b.Type != MarkdownTokenizer.TokenTypeCodeBlock)
							&& (b.StartOffset >= code.EndOffset)),
						() => $"Expected trailing content after closed fence: {Escape(prefix)}\n{Describe(blocks)}");
					sawCompleteThenTrailingText = true;
				}
			}
		}

		IsTrue(sawCodeBlockWhileOpen, () => "Never observed an open (incomplete) code block during stream.");
		IsTrue(sawCodeBlockWhileContent, () => "Never observed code block with body content mid-stream.");
		IsTrue(sawCompleteThenTrailingText, () => "Never observed closed code block followed by trailing text.");

		// Final full document: one code block + trailing text (newlines/text for "done").
		var finalBlocks = parser.Process().ToArray();
		IsTrue(finalBlocks.Count(b => b.Type == MarkdownTokenizer.TokenTypeCodeBlock) == 1);
		IsTrue(finalBlocks.Any(b => b.Type == TextProcessor.TokenTypeText || b.Type == MarkdownTokenizer.TokenTypeCodeBlock));
		var finalCode = finalBlocks.First(b => b.Type == MarkdownTokenizer.TokenTypeCodeBlock);
		var finalInfo = MarkdownRenderer.ExtractCodeBlockInfo(markdown.AsSpan(), finalCode);
		AreEqual("csharp", finalInfo.language);
		IsTrue(finalInfo.contentLength > 0);
		IsTrue(markdown.AsSpan(finalInfo.contentStartOffset, finalInfo.contentLength).ToString().Contains("var x = 1"));
	}

	/// <summary>
	/// Character stream with prose before the fence — only the fence region becomes CodeBlock.
	/// </summary>
	[TestMethod]
	public void ParseCodeBlockCharacterByCharacterAfterProse()
	{
		const string markdown =
			"""
			Here is code:
			```js
			x()
			```
			""";

		var buffer = new StringGapBuffer();
		var pool = new SpeedyQueue<Block>();
		var parser = new MarkdownParser(buffer, pool);

		var sawCodeAfterProse = false;

		for (var i = 0; i < markdown.Length; i++)
		{
			buffer.Append(markdown[i]);
			var blocks = parser.Process().ToArray();
			var prefix = markdown[..(i + 1)];
			var fenceIndex = prefix.IndexOf("```", StringComparison.Ordinal);

			if (fenceIndex < 0)
			{
				IsFalse(blocks.Any(b => b.Type == MarkdownTokenizer.TokenTypeCodeBlock),
					() => $"CodeBlock before fence exists: {Escape(prefix)}");
				continue;
			}

			// Need full 3-char fence at fenceIndex
			var fenceChars = 0;
			while (((fenceIndex + fenceChars) < prefix.Length) && (prefix[fenceIndex + fenceChars] == '`'))
			{
				fenceChars++;
			}

			if (fenceChars < 3)
			{
				IsFalse(blocks.Any(b => b.Type == MarkdownTokenizer.TokenTypeCodeBlock),
					() => $"Partial fence should not be CodeBlock: {Escape(prefix)}");
				continue;
			}

			var codeBlocks = blocks.Where(b => b.Type == MarkdownTokenizer.TokenTypeCodeBlock).ToArray();
			IsTrue(codeBlocks.Length == 1,
				() => $"Expected exactly one CodeBlock: {Escape(prefix)}\n{Describe(blocks)}");
			AreEqual(fenceIndex, codeBlocks[0].StartOffset);
			sawCodeAfterProse = true;
		}

		IsTrue(sawCodeAfterProse);
	}

	private static int CountLeadingFenceLength(string text)
	{
		var n = 0;
		while ((n < text.Length) && (text[n] == '`'))
		{
			n++;
		}

		return n;
	}

	/// <summary>
	/// True when there is non-whitespace content after the opening fence line.
	/// </summary>
	private static bool HasCodeBodyContent(string text)
	{
		var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
		var firstNl = normalized.IndexOf('\n');
		if (firstNl < 0)
		{
			return false;
		}

		for (var i = firstNl + 1; i < normalized.Length; i++)
		{
			var c = normalized[i];
			if ((c != '\n') && (c != ' ') && (c != '\t'))
			{
				// Stop if we hit a closing fence line
				if (c == '`')
				{
					var lineStart = i;
					while ((lineStart > 0) && (normalized[lineStart - 1] != '\n'))
					{
						lineStart--;
					}

					var j = lineStart;
					while ((j < normalized.Length) && ((normalized[j] == ' ') || (normalized[j] == '\t')))
					{
						j++;
					}

					var ticks = 0;
					while (((j + ticks) < normalized.Length) && (normalized[j + ticks] == '`'))
					{
						ticks++;
					}

					if (ticks >= 3)
					{
						var restOk = true;
						for (var k = j + ticks; k < normalized.Length && normalized[k] != '\n'; k++)
						{
							if ((normalized[k] != ' ') && (normalized[k] != '\t'))
							{
								restOk = false;
								break;
							}
						}

						if (restOk)
						{
							return false;
						}
					}
				}

				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// True when the stream contains a completed closing fence line for an opening ``` fence.
	/// A closer is complete as soon as the fence markers are present; trailing CR/LF after the
	/// markers is optional (matches <see cref="MarkdownFence" />).
	/// </summary>
	private static bool IsClosedFence(string text)
	{
		// Normalize line endings for simpler line checks
		var normalized = text
			.Replace("\r\n", "\n", StringComparison.Ordinal)
			.Replace('\r', '\n');

		if (!normalized.StartsWith("```", StringComparison.Ordinal))
		{
			return false;
		}

		var firstNl = normalized.IndexOf('\n');
		if (firstNl < 0)
		{
			return false;
		}

		// Opening fence length
		var openLen = 0;
		while ((openLen < normalized.Length) && (normalized[openLen] == '`'))
		{
			openLen++;
		}

		var lines = normalized.Split('\n');
		// lines[0] is opening fence line; search later lines for closer
		for (var i = 1; i < lines.Length; i++)
		{
			var line = lines[i];
			var indent = 0;
			while ((indent < line.Length) && ((line[indent] == ' ') || (line[indent] == '\t')))
			{
				indent++;
			}

			var closeLen = 0;
			while (((indent + closeLen) < line.Length) && (line[indent + closeLen] == '`'))
			{
				closeLen++;
			}

			if (closeLen < openLen)
			{
				continue;
			}

			var rest = line[(indent + closeLen)..];
			if (rest.All(static c => (c == ' ') || (c == '\t')))
			{
				return true;
			}
		}

		return false;
	}

	private static string Escape(string value)
	{
		return value
			.Replace("\r", "\\r", StringComparison.Ordinal)
			.Replace("\n", "\\n", StringComparison.Ordinal);
	}

	private static string Describe(Block[] blocks)
	{
		return string.Join("; ", blocks.Select(b => $"{b.Type}@{b.StartOffset}..{b.EndOffset}"));
	}

	[TestMethod]
	public void ParseCodeBlockClosingFenceLength()
	{
		// Closing fence must be at least as long as the opening fence.
		Process("""
				````csharp
				code with ``` inside
				````
				""",
			new Block { EndOffset = 38, Offsets = [4, 34], Type = MarkdownTokenizer.TokenTypeCodeBlock }
		);
	}

	[TestMethod]
	public void ParseHeaders()
	{
		Process("# Header", new Block { EndOffset = 8, Offsets = [1, 2], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader });
		Process("## Header", new Block { EndOffset = 9, Offsets = [2, 3], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader });
		Process("### Header", new Block { EndOffset = 10, Offsets = [3, 4], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader });
		Process("#### Header", new Block { EndOffset = 11, Offsets = [4, 5], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader });
		Process("##### Header", new Block { EndOffset = 12, Offsets = [5, 6], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader });
		Process("###### Header", new Block { EndOffset = 13, Offsets = [6, 7], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader });
	}

	[TestMethod]
	public void ParseTable()
	{
		Process("""
				| Name | Age | City |
				|-|-|-|
				| Alice | 30 | New York |
				| Bob | 25 | San Fran |
				""",
			new Block { EndOffset = 82, Type = MarkdownTokenizer.TokenTypeTable }
		);
		Process("""
				| Name  | Age | City     |
				|-------|-----|----------|
				| Alice | 30  | New York |
				| Bob   | 25  | San Fran |
				""",
			new Block { EndOffset = 110, Type = MarkdownTokenizer.TokenTypeTable }
		);
		Process("""
				## Header before table
				| Name | Age | Email |
				|-:|:-:|-:|
				| Alice | 6 | alice@domain.com |
				| Bob | 9 | bob@foo.com |
				""",
			new Block { EndOffset = 22, Offsets = [2, 3], Type = MarkdownTokenizer.TokenTypeHeader },
			new Block { EndOffset = 24, StartOffset = 22, Type = TextProcessor.TokenTypeNewLine },
			new Block { EndOffset = 120, StartOffset = 24, Type = MarkdownTokenizer.TokenTypeTable }
		);
	}

	[TestMethod]
	public void ParseUnorderedList()
	{
		Process("""
				* Item One
				* Item Two
				* Item Three
				""",
			new Block { EndOffset = 36, Type = MarkdownTokenizer.TokenTypeUnorderedList }
		);
		Process("""
				- Item One
				- Item Two
				- Item Three
				""",
			new Block { EndOffset = 36, Type = MarkdownTokenizer.TokenTypeUnorderedList }
		);
		Process("""
				+ Item One
				+ Item Two
				+ Item Three
				""",
			new Block { EndOffset = 36, Type = MarkdownTokenizer.TokenTypeUnorderedList }
		);
	}

	[TestMethod]
	public void SampleAgentOutput()
	{
		Process(
			"""
			> Can you explain c# <nullable> project element?

			The `<Nullable>` element, when added to your `.csproj` file...

			Key points about its function:

			*   **Compiler Safety:** It instructs the compiler...
			*   **Warning Generation:** When NRTs are enabled...
			*   **Implementation:** You typically add...

			    ```xml
			    <Project Sdk="Microsoft.NET.Sdk">
			      <PropertyGroup>
			        <TargetFramework>net8.0</TargetFramework>
			        <Nullable>enable</Nullable> <!-- This enables NRTs -->
			      </PropertyGroup>
			    </Project>
			    ```

			*   **Usage Example:** Without `<Nullable>`, a method returning `string` might silently pass `null`.

			Please specify what you would like to load.

			86.23 tok/sec  231 tokens  0.20s 1st/tok

			""",
			new Block { EndOffset = 48, Type = MarkdownTokenizer.TokenTypeBlockQuote },
			new Block { EndOffset = 52, StartOffset = 48, Type = TextProcessor.TokenTypeNewLine },
			new Block { EndOffset = 56, StartOffset = 52, Type = TextProcessor.TokenTypeText },
			new Block { EndOffset = 68, StartOffset = 56, Offsets = [57, 67], Type = MarkdownTokenizer.TokenTypeInlineCode },
			new Block { EndOffset = 69, StartOffset = 68, Type = TextProcessor.TokenTypeWhitespace },
			new Block { EndOffset = 97, StartOffset = 69, Type = TextProcessor.TokenTypeText },
			new Block { EndOffset = 106, StartOffset = 97, Offsets = [98, 105], Type = MarkdownTokenizer.TokenTypeInlineCode },
			new Block { EndOffset = 107, StartOffset = 106, Type = TextProcessor.TokenTypeWhitespace },
			new Block { EndOffset = 152, StartOffset = 107, Type = TextProcessor.TokenTypeText },
			new Block { EndOffset = 305, StartOffset = 152, Type = MarkdownTokenizer.TokenTypeUnorderedList },
			new Block { EndOffset = 309, StartOffset = 305, Type = TextProcessor.TokenTypeNewLine },
			new Block { EndOffset = 313, StartOffset = 309, Type = TextProcessor.TokenTypeWhitespace },
			new Block { EndOffset = 545, Offsets = [316, 542], StartOffset = 313, Type = MarkdownTokenizer.TokenTypeCodeBlock },
			new Block { EndOffset = 549, StartOffset = 545, Type = TextProcessor.TokenTypeNewLine },
			new Block { EndOffset = 649, StartOffset = 549, Type = MarkdownTokenizer.TokenTypeUnorderedList },
			new Block { EndOffset = 653, StartOffset = 649, Type = TextProcessor.TokenTypeNewLine },
			new Block { EndOffset = 742, StartOffset = 653, Type = TextProcessor.TokenTypeText }
		);
	}

	private void Process(string markdown, params Block[] expected)
	{
		var buffer = new StringGapBuffer(markdown);
		var pool = new SpeedyQueue<Block>();
		var parser = new MarkdownParser(buffer, pool);
		var blocks = parser.Process().ToArray();
		AreEqual(expected, blocks, () =>
		{
			foreach (var block in blocks)
			{
				block.DumpCSharp(x =>
				{
					x.IndentLength = 0;
					x.NewLineChars = string.Empty;
				}, ",");
			}
			return markdown;
		});
	}

	#endregion
}