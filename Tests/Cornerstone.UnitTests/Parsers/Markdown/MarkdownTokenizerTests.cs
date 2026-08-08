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
public class MarkdownTokenizerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AllExample()
	{
		Process(
			"""
			2. Thread Safety for Tool Registration
			_tools uses a standard Dictionary. If RegisterTool is called after construction, it will throw in multithreaded scenarios. Switch to ConcurrentDictionary or add a lock:
			```csharp
			private readonly ConcurrentDictionary<string, Tool> _tools = new(StringComparer.OrdinalIgnoreCase);

			public void RegisterTool(string name, Func<JsonElement, object> handler, string description, ToolJsonSchema inputSchema)
			{
			    if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tool name cannot be empty", nameof(name));
			    _tools[name] = new Tool 
			    { 
			        Handler = handler ?? throw new ArgumentNullException(nameof(handler)), 
			        Description = description ?? "No description available", 
			        InputSchema = inputSchema ?? new ToolJsonSchema() 
			    };
			}
			```
			""",
			new Token { EndOffset = 40, Type = TextProcessor.TokenTypeText },
			new Token { EndOffset = 210, StartOffset = 40, Type = TextProcessor.TokenTypeText },
			new Token { EndOffset = 823, StartOffset = 210, Type = MarkdownTokenizer.TokenTypeCodeBlock }
		);

		Process(
			"""
			# This is a header
			---

			1. C# should not trigger header
			1. And that

			*italic*

			> Block quote
			
				```CSharp
				public void Test()
				{
				}
				```
			
			More text so I can test an inline code block `Method(1, "true")`.

			""",
			new Token { EndOffset = 18, SyntaxKind = SyntaxKind.Keyword, Type = MarkdownTokenizer.TokenTypeHeader },
			new Token { EndOffset = 20, StartOffset = 18, Type = TextProcessor.TokenTypeNewLine },
			new Token { EndOffset = 23, StartOffset = 20, SyntaxKind = SyntaxKind.Operator, Type = MarkdownTokenizer.TokenTypeHorizontalRule },
			new Token { EndOffset = 27, StartOffset = 23, Type = TextProcessor.TokenTypeNewLine },
			new Token { EndOffset = 75, StartOffset = 27, Type = TextProcessor.TokenTypeText },
			new Token { EndOffset = 83, StartOffset = 75, SyntaxKind = SyntaxKind.Keyword, Type = MarkdownTokenizer.TokenTypeItalic },
			new Token { EndOffset = 87, StartOffset = 83, Type = TextProcessor.TokenTypeNewLine },
			new Token { EndOffset = 100, StartOffset = 87, Type = MarkdownTokenizer.TokenTypeBlockQuote },
			new Token { EndOffset = 104, StartOffset = 100, Type = TextProcessor.TokenTypeNewLine },
			new Token { EndOffset = 105, StartOffset = 104, Type = TextProcessor.TokenTypeWhitespace },
			new Token { EndOffset = 149, StartOffset = 105, Type = MarkdownTokenizer.TokenTypeCodeBlock },
			new Token { EndOffset = 153, StartOffset = 149, Type = TextProcessor.TokenTypeNewLine },
			new Token { EndOffset = 198, StartOffset = 153, Type = TextProcessor.TokenTypeText },
			new Token { EndOffset = 217, StartOffset = 198, Type = MarkdownTokenizer.TokenTypeInlineCode },
			new Token { EndOffset = 220, StartOffset = 217, Type = TextProcessor.TokenTypeText }
		);
	}

	[TestMethod]
	public void BasicExample()
	{
		var scenarios = new (string markdown, Token[] expected)[]
		{
			("# Header 1", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 10, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("## Header 2", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 11, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("### Header 3", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 12, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("#### Header 4", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 13, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("##### Header 5", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 14, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("###### Header 6", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 15, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("---", [new Token { SyntaxKind = SyntaxKind.Operator, EndOffset = 3, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHorizontalRule }]),
			("**bold**", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 8, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBold }]),
			("__bold__", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 8, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBold }]),
			("*italic*", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 8, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeItalic }]),
			("_italic_", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 8, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeItalic }]),
			("***bold/italic***", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 17, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBoldAndItalic }]),
			("___bold/italic___", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 17, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBoldAndItalic }]),
			("~~strikethrough~~", [new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 17, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeStrikethrough }]),
			("> BlockQuote", [new Token { EndOffset = 12, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBlockQuote }])
		};

		foreach (var scenario in scenarios)
		{
			Process(scenario.markdown, scenario.expected);
		}
	}

	[TestMethod]
	public void BlockQuotes()
	{
		Process("> Block Quote", new Token { EndOffset = 13, Type = MarkdownTokenizer.TokenTypeBlockQuote });
		Process("	> Block Quote",
			new Token { EndOffset = 1, Type = TextProcessor.TokenTypeWhitespace },
			new Token { EndOffset = 14, StartOffset = 1, Type = MarkdownTokenizer.TokenTypeBlockQuote }
		);
		Process("""
				> Quotes should be able
				> to go many lines
				> even more lines
				""",
			new Token { EndOffset = 62, Type = MarkdownTokenizer.TokenTypeBlockQuote }
		);
	}

	[TestMethod]
	public void CodeBlocks()
	{
		Process(
			"""
			```CSharp
			public void Test()
			{
				Console.WriteLine("Test");
			}
			```
			""",
			new Token { EndOffset = 69, Type = MarkdownTokenizer.TokenTypeCodeBlock }
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
			new Token { EndOffset = 36, Type = MarkdownTokenizer.TokenTypeUnorderedList }
		);
		Process("""
				- Item One
				- Item Two
				- Item Three
				""",
			new Token { EndOffset = 36, Type = MarkdownTokenizer.TokenTypeUnorderedList }
		);
		Process("""
				+ Item One
				+ Item Two
				+ Item Three
				""",
			new Token { EndOffset = 36, Type = MarkdownTokenizer.TokenTypeUnorderedList }
		);
	}

	private void Process(string markdown, params Token[] expected)
	{
		var buffer = new StringGapBuffer(markdown);
		var pool = new SpeedyQueue<Token>();
		var tokenizer = new MarkdownTokenizer(buffer, pool);
		var tokens = tokenizer.Process().ToArray();
		AreEqual(expected, tokens, () =>
		{
			foreach (var token in tokens)
			{
				token.DumpCSharp(x =>
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