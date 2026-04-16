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
			# This is a header
			---

			1. C# should not trigger header
			1. And that

			*italic*

			> Block quote

			""",
			new Token { Color = SyntaxColor.Keyword, EndOffset = 18, Type = MarkdownTokenizer.TokenTypeHeader },
			new Token { EndOffset = 20, StartOffset = 18, Type = TextProcessor.TokenTypeNewLine },
			new Token { Color = SyntaxColor.Operator, EndOffset = 23, StartOffset = 20, Type = MarkdownTokenizer.TokenTypeHorizontalRule },
			new Token { EndOffset = 27, StartOffset = 23, Type = TextProcessor.TokenTypeNewLine },
			new Token { EndOffset = 75, StartOffset = 27 },
			new Token { Color = SyntaxColor.Keyword, EndOffset = 83, StartOffset = 75, Type = MarkdownTokenizer.TokenTypeItalic },
			new Token { EndOffset = 87, StartOffset = 83, Type = TextProcessor.TokenTypeNewLine },
			new Token { EndOffset = 100, StartOffset = 87, Type = MarkdownTokenizer.TokenTypeBlockQuote },
			new Token { EndOffset = 102, StartOffset = 100, Type = TextProcessor.TokenTypeNewLine }
		);
	}

	[TestMethod]
	public void BasicExample()
	{
		var scenarios = new (string markdown, Token[] expected)[]
		{
			("# Header 1", [new Token { Color = SyntaxColor.Keyword, EndOffset = 10, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("## Header 2", [new Token { Color = SyntaxColor.Keyword, EndOffset = 11, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("### Header 3", [new Token { Color = SyntaxColor.Keyword, EndOffset = 12, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("#### Header 4", [new Token { Color = SyntaxColor.Keyword, EndOffset = 13, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("##### Header 5", [new Token { Color = SyntaxColor.Keyword, EndOffset = 14, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("###### Header 6", [new Token { Color = SyntaxColor.Keyword, EndOffset = 15, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHeader }]),
			("---", [new Token { Color = SyntaxColor.Operator, EndOffset = 3, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeHorizontalRule }]),
			("**bold**", [new Token { Color = SyntaxColor.Keyword, EndOffset = 8, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBold }]),
			("__bold__", [new Token { Color = SyntaxColor.Keyword, EndOffset = 8, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBold }]),
			("*italic*", [new Token { Color = SyntaxColor.Keyword, EndOffset = 8, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeItalic }]),
			("_italic_", [new Token { Color = SyntaxColor.Keyword, EndOffset = 8, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeItalic }]),
			("***bold/italic***", [new Token { Color = SyntaxColor.Keyword, EndOffset = 17, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBoldAndItalic }]),
			("___bold/italic___", [new Token { Color = SyntaxColor.Keyword, EndOffset = 17, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeBoldAndItalic }]),
			("~~strikethrough~~", [new Token { Color = SyntaxColor.Keyword, EndOffset = 17, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeStrikethrough }]),
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
		Process("> Block Quote", new Token { EndOffset = 13,  Type = MarkdownTokenizer.TokenTypeBlockQuote });
		Process("	> Block Quote",
			new Token { EndOffset = 1, Type = Tokenizer.TokenTypeWhitespace },
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
			new Token { Color = SyntaxColor.None, EndOffset = 69, StartOffset = 0, Type = MarkdownTokenizer.TokenTypeCodeBlock }
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