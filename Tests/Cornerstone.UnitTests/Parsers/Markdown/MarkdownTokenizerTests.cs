#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Automation;
using Cornerstone.Automation.Web;
using Cornerstone.Automation.Web.Browsers;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Html;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Profiling;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Markdown;

[TestClass]
public class MarkdownTokenizerTests : TokenizerTest
{
	#region Methods

	[TestMethod]
	public void AllTokens()
	{
		var expected = GetExpectedTokens();
		var t = new MarkdownTokenizer();
		t.Add(GetContentToTokenize());
		var actual = t.GetTokens().ToArray();
		CopyToClipboard(actual.DumpCSharpArray());
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void GenerateExpectedTokens()
	{
		CreateExpectedTokens<MarkdownTokenizer, MarkdownTokenData, MarkdownTokenType>(
			"Markdown",
			$"{nameof(MarkdownTokenizerTests)}.cs",
			EnableFileUpdates || IsDebugging
		);
	}

	[TestMethod]
	public void Headers()
	{
		var scenarios = new (string input, string html, string syntax, MarkdownTokenData expected)[]
		{
			("# Header", "#", "", new MarkdownTokenData { ColumnNumber = 1, EndIndex = 8, LineNumber = 1, TokenIndexes = [1, 0, 0, 8], Type = MarkdownTokenType.Header, ElementName = "h1" })
		};

		foreach (var scenario in scenarios)
		{
			var tokenizer = new MarkdownTokenizer();
			tokenizer.Add(scenario.input);
			var actual = tokenizer.GetTokens().First();
			AreEqual(scenario.expected, actual, actual.DumpCSharp());
		}
	}

	[TestMethod]
	public void InvalidStart()
	{
		var scenarios = new (string input, string html, string syntax, MarkdownTokenData expected)[]
		{
			("#", "#", "", new MarkdownTokenData { ColumnNumber = 1, EndIndex = 1, ElementName = "h1", LineNumber = 1, TokenIndexes = [1, 0, 0, 0], Type = MarkdownTokenType.Header }),
			("a", "a", "", new MarkdownTokenData { ColumnNumber = 1, EndIndex = 1, LineNumber = 1, Type = MarkdownTokenType.Text }),
			("-", "-", "", new MarkdownTokenData { ColumnNumber = 1, EndIndex = 1, LineNumber = 1, Type = MarkdownTokenType.Text })
		};

		foreach (var scenario in scenarios)
		{
			var tokenizer = new MarkdownTokenizer();
			tokenizer.Add(scenario.input);
			var actual = tokenizer.GetTokens().First();
			AreEqual(scenario.expected, actual, actual.DumpCSharp());
		}
	}

	[TestMethod]
	public void SimpleTokens()
	{
		var scenarios = new (string input, string html, string syntax, MarkdownTokenData expected)[]
		{
			//   01234 5 67890 1 234
			(
				"# header",
				"<h1>header</h1>",
				"# header",
				new MarkdownTokenData { ColumnNumber = 2, ElementName = "h1", EndIndex = 9, LineNumber = 1, StartIndex = 1, TokenIndexes = [2, 0, 0, 9], Type = MarkdownTokenType.Header }
			),
			(
				"```\r\naoeu\r\n```",
				"<pre>aoeu</pre>",
				"<span class=\"syntaxKeywordMuted\">```\r\n</span>aoeu<span class=\"syntaxKeywordMuted\">\r\n```</span>",
				new MarkdownTokenData { ColumnNumber = 2, ElementName = "pre", EndIndex = 15, LineNumber = 1, StartIndex = 1, TokenIndexes = [1, 6, 9, 12], Type = MarkdownTokenType.Code }
			),
			(
				"___Bold + Italic Hello World___",
				"<em><strong>Bold + Italic Hello World</strong></em>",
				"<span class=\"syntaxParameter\">___</span>Bold + Italic Hello World<span class=\"syntaxParameter\">___</span>",
				new MarkdownTokenData { ColumnNumber = 2, ElementName = "strong", EndIndex = 32, LineNumber = 1, StartIndex = 1, TokenIndexes = [4, 29], Type = MarkdownTokenType.Bold }
			),
			(
				"***test***",
				"<em><strong>test</strong></em>",
				"<span class=\"syntaxParameter\">***</span>test<span class=\"syntaxParameter\">***</span>",
				new MarkdownTokenData { ColumnNumber = 2, ElementName = "strong", EndIndex = 11, LineNumber = 1, StartIndex = 1, TokenIndexes = [4, 8], Type = MarkdownTokenType.Bold }
			),
			(
				"_test_",
				"<em>test</em>",
				"<span class=\"syntaxParameter\">_</span>test<span class=\"syntaxParameter\">_</span>",
				new MarkdownTokenData { ColumnNumber = 2, ElementName = "em", EndIndex = 7, LineNumber = 1, StartIndex = 1, TokenIndexes = [2, 6], Type = MarkdownTokenType.Italic }
			),
			(
				"*test*",
				"<em>test</em>",
				"<span class=\"syntaxParameter\">*</span>test<span class=\"syntaxParameter\">*</span>",
				new MarkdownTokenData { ColumnNumber = 2, ElementName = "em", EndIndex = 7, LineNumber = 1, StartIndex = 1, TokenIndexes = [2, 6], Type = MarkdownTokenType.Italic }
			),
			(
				"__test__",
				"<strong>test</strong>",
				"<span class=\"syntaxParameter\">__</span>test<span class=\"syntaxParameter\">__</span>",
				new MarkdownTokenData { ColumnNumber = 2, ElementName = "strong", EndIndex = 9, LineNumber = 1, StartIndex = 1, TokenIndexes = [3, 7], Type = MarkdownTokenType.Bold }
			),
			(
				"**test**",
				"<strong>test</strong>",
				"<span class=\"syntaxParameter\">**</span>test<span class=\"syntaxParameter\">**</span>",
				new MarkdownTokenData { ColumnNumber = 2, ElementName = "strong", EndIndex = 9, LineNumber = 1, StartIndex = 1, TokenIndexes = [3, 7], Type = MarkdownTokenType.Bold }
			)
		};

		foreach (var scenario in scenarios)
		{
			var tokenizer = new MarkdownTokenizer();
			tokenizer.Add("\t" + scenario.input);
			var actual = tokenizer.GetTokens().Skip(1).First();
			AreEqual(scenario.expected, actual, actual.DumpCSharp());

			var writer = new HtmlWriter();
			actual.WriteTo(writer);
			AreEqual(scenario.html, writer.ToString(), () => "html");

			tokenizer = new MarkdownTokenizer();
			tokenizer.Add(scenario.input);
			AreEqual(scenario.syntax, tokenizer.ToCodeSyntaxHtml(), () => "syntax");
		}
	}

	[TestMethod]
	public void ToCodeSyntaxHtml()
	{
		var timer = Timer.StartNewTimer();
		var json = GetContentToTokenize();
		var actual = Tokenizer.ToCodeSyntaxHtml<MarkdownTokenizer>(json, true);
		timer.Elapsed.Dump();
		actual.Dump();

		if (EnableBrowserSamples)
		{
			using var browser = Chrome.AttachOrCreate();
			browser.AutoClose = false;
			browser.SetHtml(actual);
		}
	}

	[TestMethod]
	public void ToHtml()
	{
		if (!EnableBrowserSamples)
		{
			return;
		}

		var markdown = GetContentToTokenize();
		var actual = MarkdownTokenizer.ToHtml(markdown);
		HtmlWriter.WrapHtmlSnippet(actual).Dump();
		actual.DumpInBrowser(BrowserType.Chrome);
	}

	protected override string GetContentToTokenize()
	{
		return """
				> # Test

				This is unknown code.

				```
				1234567890 liO0
				```

				``` CSharp
				public void Test()
				{
					System.Console.WriteLine("This is a test");
				}
				```

				``` json
				{
					"Foo": "Bar",
					"Number": 1.234,
					"Enabled": true,
					"Options": ["Foo", 3.21, false]
				}
				```

				# Bold Text

				**Bold Foo Bar**
				__Bold Hello World__

				Content**can**be bold without spaces.

				# Italic Test

				*Italic Foo Bar*
				_Italic Hello World_

				# Bold & Italic Text

				***Bold + Italic Foo Bar***
				___Bold + Italic Hello World___

				# Header 1
				## Header 2
				### Header 3
				#### Header 4
				##### Header 5
				###### Header 6

				> Header below should have the class applied
				> Should be able to have two lines?

				# Header 1 {syntaxArgument}
				## Header 2 {syntaxCommand}
				### Header 3 {syntaxComment}
				#### Header 4 {syntaxIdentifier}
				##### Header 5 {syntaxKeyword}
				###### Header 6 {syntaxNumber}

				The quick brown fox jumped over the lazy dog's back.

				This is more text.

				- Option 1
				- Option 2
				  - 2a Sub Option
				  - 2b Sub Option
				- Option 3

				aoeu

				1. Option One
				1. Option Two
				1. Option Three
				1. Option Four

				[Test](http://test.com)
				[ Test ]( http://test.com )

				![Image](http://test.com/test.png)

				// Incorrect Links :)

				(Test)( http://test.com )

				""";
	}

	private MarkdownTokenData[] GetExpectedTokens()
	{
		var response = new List<MarkdownTokenData>();

		response.AddRange([
			// <Scenarios>
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "blockquote", EndIndex = 8, LineNumber = 1, TokenIndexes = [1], Type = MarkdownTokenType.BlockQuote },
			new MarkdownTokenData { ColumnNumber = 9, EndIndex = 12, LineNumber = 1, StartIndex = 8, TokenIndexes = [1], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, EndIndex = 33, LineNumber = 3, StartIndex = 12, TokenIndexes = [1], Type = MarkdownTokenType.Text },
			new MarkdownTokenData { ColumnNumber = 22, EndIndex = 37, LineNumber = 3, StartIndex = 33, TokenIndexes = [1], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "pre", EndIndex = 62, LineNumber = 5, StartIndex = 37, TokenIndexes = [37, 42, 56, 59], Type = MarkdownTokenType.Code },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 66, LineNumber = 5, StartIndex = 62, TokenIndexes = [37, 42, 56, 59], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "pre", EndIndex = 153, LineNumber = 7, StartIndex = 66, TokenIndexes = [66, 69, 75, 78, 147, 150], Type = MarkdownTokenType.Code },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 157, LineNumber = 7, StartIndex = 153, TokenIndexes = [66, 69, 75, 78, 147, 150], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "pre", EndIndex = 264, LineNumber = 9, StartIndex = 157, TokenIndexes = [157, 160, 164, 167, 258, 261], Type = MarkdownTokenType.Code },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 268, LineNumber = 9, StartIndex = 264, TokenIndexes = [157, 160, 164, 167, 258, 261], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h1", EndIndex = 279, LineNumber = 11, StartIndex = 268, TokenIndexes = [269, 0, 0, 278], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 283, LineNumber = 11, StartIndex = 279, TokenIndexes = [269, 0, 0, 278], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "strong", EndIndex = 299, LineNumber = 13, StartIndex = 283, TokenIndexes = [285, 297], Type = MarkdownTokenType.Bold },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 301, LineNumber = 13, StartIndex = 299, TokenIndexes = [285, 297], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "strong", EndIndex = 321, LineNumber = 14, StartIndex = 301, TokenIndexes = [303, 319], Type = MarkdownTokenType.Bold },
			new MarkdownTokenData { ColumnNumber = 21, EndIndex = 325, LineNumber = 14, StartIndex = 321, TokenIndexes = [303, 319], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, EndIndex = 332, LineNumber = 16, StartIndex = 325, TokenIndexes = [303, 319], Type = MarkdownTokenType.Text },
			new MarkdownTokenData { ColumnNumber = 8, ElementName = "strong", EndIndex = 339, LineNumber = 16, StartIndex = 332, TokenIndexes = [334, 337], Type = MarkdownTokenType.Bold },
			new MarkdownTokenData { ColumnNumber = 9, EndIndex = 362, LineNumber = 16, StartIndex = 339, TokenIndexes = [334, 337], Type = MarkdownTokenType.Text },
			new MarkdownTokenData { ColumnNumber = 32, EndIndex = 366, LineNumber = 16, StartIndex = 362, TokenIndexes = [334, 337], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h1", EndIndex = 379, LineNumber = 18, StartIndex = 366, TokenIndexes = [367, 0, 0, 378], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 383, LineNumber = 18, StartIndex = 379, TokenIndexes = [367, 0, 0, 378], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "em", EndIndex = 399, LineNumber = 20, StartIndex = 383, TokenIndexes = [384, 398], Type = MarkdownTokenType.Italic },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 401, LineNumber = 20, StartIndex = 399, TokenIndexes = [384, 398], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "em", EndIndex = 421, LineNumber = 21, StartIndex = 401, TokenIndexes = [402, 420], Type = MarkdownTokenType.Italic },
			new MarkdownTokenData { ColumnNumber = 21, EndIndex = 425, LineNumber = 21, StartIndex = 421, TokenIndexes = [402, 420], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h1", EndIndex = 445, LineNumber = 23, StartIndex = 425, TokenIndexes = [426, 0, 0, 444], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 449, LineNumber = 23, StartIndex = 445, TokenIndexes = [426, 0, 0, 444], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "strong", EndIndex = 476, LineNumber = 25, StartIndex = 449, TokenIndexes = [452, 473], Type = MarkdownTokenType.Bold },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 478, LineNumber = 25, StartIndex = 476, TokenIndexes = [452, 473], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "strong", EndIndex = 509, LineNumber = 26, StartIndex = 478, TokenIndexes = [481, 506], Type = MarkdownTokenType.Bold },
			new MarkdownTokenData { ColumnNumber = 32, EndIndex = 513, LineNumber = 26, StartIndex = 509, TokenIndexes = [481, 506], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h1", EndIndex = 523, LineNumber = 28, StartIndex = 513, TokenIndexes = [514, 0, 0, 522], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 525, LineNumber = 28, StartIndex = 523, TokenIndexes = [514, 0, 0, 522], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h2", EndIndex = 536, LineNumber = 29, StartIndex = 525, TokenIndexes = [527, 0, 0, 535], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 538, LineNumber = 29, StartIndex = 536, TokenIndexes = [527, 0, 0, 535], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h3", EndIndex = 550, LineNumber = 30, StartIndex = 538, TokenIndexes = [541, 0, 0, 549], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 552, LineNumber = 30, StartIndex = 550, TokenIndexes = [541, 0, 0, 549], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h4", EndIndex = 565, LineNumber = 31, StartIndex = 552, TokenIndexes = [556, 0, 0, 564], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 567, LineNumber = 31, StartIndex = 565, TokenIndexes = [556, 0, 0, 564], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h5", EndIndex = 581, LineNumber = 32, StartIndex = 567, TokenIndexes = [572, 0, 0, 580], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 583, LineNumber = 32, StartIndex = 581, TokenIndexes = [572, 0, 0, 580], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h6", EndIndex = 598, LineNumber = 33, StartIndex = 583, TokenIndexes = [589, 0, 0, 597], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 602, LineNumber = 33, StartIndex = 598, TokenIndexes = [589, 0, 0, 597], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "blockquote", EndIndex = 646, LineNumber = 35, StartIndex = 602, TokenIndexes = [603], Type = MarkdownTokenType.BlockQuote },
			new MarkdownTokenData { ColumnNumber = 45, EndIndex = 648, LineNumber = 35, StartIndex = 646, TokenIndexes = [603], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "blockquote", EndIndex = 683, LineNumber = 36, StartIndex = 648, TokenIndexes = [649], Type = MarkdownTokenType.BlockQuote },
			new MarkdownTokenData { ColumnNumber = 36, EndIndex = 687, LineNumber = 36, StartIndex = 683, TokenIndexes = [649], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h1", EndIndex = 714, LineNumber = 38, StartIndex = 687, TokenIndexes = [688, 698, 713, 713], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 716, LineNumber = 38, StartIndex = 714, TokenIndexes = [688, 698, 713, 713], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h2", EndIndex = 743, LineNumber = 39, StartIndex = 716, TokenIndexes = [718, 728, 742, 742], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 745, LineNumber = 39, StartIndex = 743, TokenIndexes = [718, 728, 742, 742], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h3", EndIndex = 773, LineNumber = 40, StartIndex = 745, TokenIndexes = [748, 758, 772, 772], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 775, LineNumber = 40, StartIndex = 773, TokenIndexes = [748, 758, 772, 772], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h4", EndIndex = 807, LineNumber = 41, StartIndex = 775, TokenIndexes = [779, 789, 806, 806], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 809, LineNumber = 41, StartIndex = 807, TokenIndexes = [779, 789, 806, 806], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h5", EndIndex = 839, LineNumber = 42, StartIndex = 809, TokenIndexes = [814, 824, 838, 838], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 841, LineNumber = 42, StartIndex = 839, TokenIndexes = [814, 824, 838, 838], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "h6", EndIndex = 871, LineNumber = 43, StartIndex = 841, TokenIndexes = [847, 857, 870, 870], Type = MarkdownTokenType.Header },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 875, LineNumber = 43, StartIndex = 871, TokenIndexes = [847, 857, 870, 870], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, EndIndex = 927, LineNumber = 45, StartIndex = 875, TokenIndexes = [847, 857, 870, 870], Type = MarkdownTokenType.Text },
			new MarkdownTokenData { ColumnNumber = 53, EndIndex = 931, LineNumber = 45, StartIndex = 927, TokenIndexes = [847, 857, 870, 870], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, EndIndex = 949, LineNumber = 47, StartIndex = 931, TokenIndexes = [847, 857, 870, 870], Type = MarkdownTokenType.Text },
			new MarkdownTokenData { ColumnNumber = 19, EndIndex = 953, LineNumber = 47, StartIndex = 949, TokenIndexes = [847, 857, 870, 870], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "li", EndIndex = 963, LineNumber = 49, StartIndex = 953, TokenIndexes = [955], Type = MarkdownTokenType.UnorderedList },
			new MarkdownTokenData { ColumnNumber = 11, EndIndex = 965, LineNumber = 49, StartIndex = 963, TokenIndexes = [955], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "li", EndIndex = 975, LineNumber = 50, StartIndex = 965, TokenIndexes = [967], Type = MarkdownTokenType.UnorderedList },
			new MarkdownTokenData { ColumnNumber = 11, EndIndex = 977, LineNumber = 50, StartIndex = 975, TokenIndexes = [967], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, EndIndex = 979, LineNumber = 51, StartIndex = 977, TokenIndexes = [967], Type = MarkdownTokenType.Whitespace },
			new MarkdownTokenData { ColumnNumber = 3, ElementName = "li", EndIndex = 994, LineNumber = 51, StartIndex = 979, TokenIndexes = [981], Type = MarkdownTokenType.UnorderedList },
			new MarkdownTokenData { ColumnNumber = 18, EndIndex = 996, LineNumber = 51, StartIndex = 994, TokenIndexes = [981], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, EndIndex = 998, LineNumber = 52, StartIndex = 996, TokenIndexes = [981], Type = MarkdownTokenType.Whitespace },
			new MarkdownTokenData { ColumnNumber = 3, ElementName = "li", EndIndex = 1013, LineNumber = 52, StartIndex = 998, TokenIndexes = [1000], Type = MarkdownTokenType.UnorderedList },
			new MarkdownTokenData { ColumnNumber = 18, EndIndex = 1015, LineNumber = 52, StartIndex = 1013, TokenIndexes = [1000], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "li", EndIndex = 1025, LineNumber = 53, StartIndex = 1015, TokenIndexes = [1017], Type = MarkdownTokenType.UnorderedList },
			new MarkdownTokenData { ColumnNumber = 11, EndIndex = 1029, LineNumber = 53, StartIndex = 1025, TokenIndexes = [1017], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, EndIndex = 1033, LineNumber = 55, StartIndex = 1029, TokenIndexes = [1017], Type = MarkdownTokenType.Text },
			new MarkdownTokenData { ColumnNumber = 5, EndIndex = 1037, LineNumber = 55, StartIndex = 1033, TokenIndexes = [1017], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "li", EndIndex = 1050, LineNumber = 57, StartIndex = 1037, TokenIndexes = [1017], Type = MarkdownTokenType.OrderedList },
			new MarkdownTokenData { ColumnNumber = 14, EndIndex = 1052, LineNumber = 57, StartIndex = 1050, TokenIndexes = [1017], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "li", EndIndex = 1065, LineNumber = 58, StartIndex = 1052, TokenIndexes = [1017], Type = MarkdownTokenType.OrderedList },
			new MarkdownTokenData { ColumnNumber = 14, EndIndex = 1067, LineNumber = 58, StartIndex = 1065, TokenIndexes = [1017], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "li", EndIndex = 1082, LineNumber = 59, StartIndex = 1067, TokenIndexes = [1017], Type = MarkdownTokenType.OrderedList },
			new MarkdownTokenData { ColumnNumber = 16, EndIndex = 1084, LineNumber = 59, StartIndex = 1082, TokenIndexes = [1017], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "li", EndIndex = 1098, LineNumber = 60, StartIndex = 1084, TokenIndexes = [1017], Type = MarkdownTokenType.OrderedList },
			new MarkdownTokenData { ColumnNumber = 15, EndIndex = 1102, LineNumber = 60, StartIndex = 1098, TokenIndexes = [1017], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "a", EndIndex = 1125, LineNumber = 62, StartIndex = 1102, TokenIndexes = [1102, 1107, 1108, 1124], Type = MarkdownTokenType.Link },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 1127, LineNumber = 62, StartIndex = 1125, TokenIndexes = [1102, 1107, 1108, 1124], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, ElementName = "a", EndIndex = 1154, LineNumber = 63, StartIndex = 1127, TokenIndexes = [1127, 1134, 1135, 1153], Type = MarkdownTokenType.Link },
			new MarkdownTokenData { ColumnNumber = 2, EndIndex = 1158, LineNumber = 63, StartIndex = 1154, TokenIndexes = [1127, 1134, 1135, 1153], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, EndIndex = 1192, LineNumber = 65, StartIndex = 1158, TokenIndexes = [1127, 1134, 1135, 1153], Type = MarkdownTokenType.Text },
			new MarkdownTokenData { ColumnNumber = 35, EndIndex = 1196, LineNumber = 65, StartIndex = 1192, TokenIndexes = [1127, 1134, 1135, 1153], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, EndIndex = 1217, LineNumber = 67, StartIndex = 1196, TokenIndexes = [1127, 1134, 1135, 1153], Type = MarkdownTokenType.Text },
			new MarkdownTokenData { ColumnNumber = 22, EndIndex = 1221, LineNumber = 67, StartIndex = 1217, TokenIndexes = [1127, 1134, 1135, 1153], Type = MarkdownTokenType.NewLine },
			new MarkdownTokenData { ColumnNumber = 1, EndIndex = 1246, LineNumber = 69, StartIndex = 1221, TokenIndexes = [1127, 1134, 1135, 1153], Type = MarkdownTokenType.Text },
			new MarkdownTokenData { ColumnNumber = 26, EndIndex = 1248, LineNumber = 69, StartIndex = 1246, TokenIndexes = [1127, 1134, 1135, 1153], Type = MarkdownTokenType.NewLine }
			// </Scenarios>
		]);

		return response.ToArray();
	}

	#endregion
}