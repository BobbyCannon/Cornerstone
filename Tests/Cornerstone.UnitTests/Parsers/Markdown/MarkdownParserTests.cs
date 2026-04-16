#region References

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
			""",
			new Block { EndOffset = 62, Type = MarkdownTokenizer.TokenTypeBlockQuote }
		);
	}
	
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
	public void ParseBoldAndItalic()
	{
		Process("*Italic*", new Block { EndOffset = 8, Offsets = [1, 7], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeItalic });
		Process("_Italic_", new Block { EndOffset = 8, Offsets = [1, 7], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeItalic });
		Process("**Bold**", new Block { EndOffset = 8, Offsets = [2, 6], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBold });
		Process("__Bold__", new Block { EndOffset = 8, Offsets = [2, 6], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBold });
		Process("***BoldAndItalic***", new Block { EndOffset = 19, Offsets = [3, 16], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBoldAndItalic });
		Process("___BoldAndItalic___", new Block { EndOffset = 19, Offsets = [3, 16], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBoldAndItalic });
		Process("~~StrikeThrough~~", new Block { EndOffset = 17, Offsets = [2, 15], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeStrikethrough });
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
			new Block { EndOffset = 76, Offsets = [3, 73], StartOffset = 0, Type = MarkdownTokenizer.TokenTypeCodeBlock }
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