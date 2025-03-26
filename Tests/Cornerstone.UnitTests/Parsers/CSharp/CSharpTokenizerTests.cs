#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Automation.Web.Browsers;
using Cornerstone.Extensions;
using Cornerstone.Parsers;
using Cornerstone.Parsers.CSharp;
using Cornerstone.Profiling;
using Cornerstone.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.CSharp;

[TestClass]
public class CSharpTokenizerTests : TokenizerTest
{
	#region Methods

	[TestMethod]
	public void AllTokens()
	{
		var expected = GetExpectedTokens();
		var t = new CSharpTokenizer();
		t.Add(GetContentToTokenize());
		var actual = t.GetTokens().ToArray();
		CopyToClipboard(actual.DumpCSharpArray());
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void Debugging()
	{
		var t = new CSharpTokenizer();
		//t.Add(GetContentToTokenize());
		//              1          2            3             4
		//     0123456789012345678 9 0 1 23456789012345 6 7 8 9012345678
		t.Add("""
			#region References

			using System;

			#endregion

			namespace Test.Assembly;

			public static class MyTestExtensions
			{
				public static string GetString(this MyTests test)
				{
					return test.ToString();
				}
			}

			[TestClass]
			public class MyTests : CornerstoneTests
			{
				public int Add(int a, int b)
				{
					return a + b;
				}
			}

			""");
		t.ToString().Escape().Dump();

		var actual = t.GetTokens().ToArray();
		actual.ForEach(x =>
		{
			x.Value = x.Value switch
			{
				SyntaxTrivia trivia => trivia.Kind().ToString(),
				SyntaxToken token => token.Kind().ToString(),
				_ => null
			};
		});
		actual.DumpPrettyJson();

		var syntaxHtml = t.ToCodeSyntaxHtml(true);
		syntaxHtml.Dump();

		if (EnableBrowserSamples)
		{
			using var browser = Chrome.AttachOrCreate();
			browser.AutoClose = false;
			browser.SetHtml(syntaxHtml);
		}
	}

	[TestMethod]
	public void GenerateExpectedTokens()
	{
		CreateExpectedTokens<CSharpTokenizer, CSharpTokenData, SyntaxKind>(
			"CSharp",
			$"{nameof(CSharpTokenizerTests)}.cs",
			EnableFileUpdates || IsDebugging
		);
	}

	[TestMethod]
	public void ToCodeSyntaxHtml()
	{
		var timer = Timer.StartNewTimer();
		var code = GetContentToTokenize();
		var actual = Tokenizer.ToCodeSyntaxHtml<CSharpTokenizer>(code, true);
		timer.Elapsed.Dump();
		actual.Dump();

		if (EnableBrowserSamples || IsDebugging)
		{
			using var browser = Chrome.AttachOrCreate();
			browser.AutoClose = false;
			browser.SetHtml(actual);
		}
	}

	protected override string GetContentToTokenize()
	{
		return """
				#region References

				using System;

				#endregion

				namespace MyTest.Assembly;

				public class MyClass : BaseClass
				{
					// This is a comment
				}
				""";
	}

	private CSharpTokenData[] GetExpectedTokens()
	{
		var response = new List<CSharpTokenData>();

		response.AddRange([
			// <Scenarios>
			new CSharpTokenData { EndIndex = 18, TokenIndexes = [0, 1, 7, 8], Type = SyntaxKind.RegionDirectiveTrivia },
			new CSharpTokenData { EndIndex = 20, StartIndex = 18, Type = SyntaxKind.EndOfLineTrivia },
			new CSharpTokenData { EndIndex = 22, StartIndex = 20, Type = SyntaxKind.EndOfLineTrivia },
			new CSharpTokenData { EndIndex = 35, StartIndex = 22, TokenIndexes = [22, 27, 28, 34], Type = SyntaxKind.UsingDirective },
			new CSharpTokenData { EndIndex = 37, StartIndex = 35, Type = SyntaxKind.EndOfLineTrivia },
			new CSharpTokenData { EndIndex = 39, StartIndex = 37, Type = SyntaxKind.EndOfLineTrivia },
			new CSharpTokenData { EndIndex = 49, StartIndex = 39, TokenIndexes = [39, 40], Type = SyntaxKind.EndRegionDirectiveTrivia },
			new CSharpTokenData { EndIndex = 51, StartIndex = 49, Type = SyntaxKind.EndOfLineTrivia },
			new CSharpTokenData { EndIndex = 53, StartIndex = 51, Type = SyntaxKind.EndOfLineTrivia },
			new CSharpTokenData { EndIndex = 62, StartIndex = 53, Type = SyntaxKind.NamespaceKeyword },
			new CSharpTokenData { EndIndex = 63, StartIndex = 62, Type = SyntaxKind.WhitespaceTrivia },
			new CSharpTokenData { EndIndex = 78, StartIndex = 63, TokenIndexes = [63, 69, 70], Type = SyntaxKind.QualifiedName },
			new CSharpTokenData { EndIndex = 79, StartIndex = 78, Type = SyntaxKind.SemicolonToken },
			new CSharpTokenData { EndIndex = 81, StartIndex = 79, Type = SyntaxKind.EndOfLineTrivia },
			new CSharpTokenData { EndIndex = 83, StartIndex = 81, Type = SyntaxKind.EndOfLineTrivia },
			new CSharpTokenData { EndIndex = 89, StartIndex = 83, Type = SyntaxKind.PublicKeyword },
			new CSharpTokenData { EndIndex = 90, StartIndex = 89, Type = SyntaxKind.WhitespaceTrivia },
			new CSharpTokenData { EndIndex = 95, StartIndex = 90, Type = SyntaxKind.ClassKeyword },
			new CSharpTokenData { EndIndex = 96, StartIndex = 95, Type = SyntaxKind.WhitespaceTrivia },
			new CSharpTokenData { EndIndex = 103, StartIndex = 96, Type = SyntaxKind.IdentifierToken },
			new CSharpTokenData { EndIndex = 104, StartIndex = 103, Type = SyntaxKind.WhitespaceTrivia },
			new CSharpTokenData { EndIndex = 105, StartIndex = 104, Type = SyntaxKind.ColonToken },
			new CSharpTokenData { EndIndex = 106, StartIndex = 105, Type = SyntaxKind.WhitespaceTrivia },
			new CSharpTokenData { EndIndex = 115, StartIndex = 106, TokenIndexes = [106], Type = SyntaxKind.IdentifierToken },
			new CSharpTokenData { EndIndex = 117, StartIndex = 115, Type = SyntaxKind.EndOfLineTrivia },
			new CSharpTokenData { EndIndex = 118, StartIndex = 117, Type = SyntaxKind.OpenBraceToken },
			new CSharpTokenData { EndIndex = 120, StartIndex = 118, Type = SyntaxKind.EndOfLineTrivia },
			new CSharpTokenData { EndIndex = 121, StartIndex = 120, Type = SyntaxKind.WhitespaceTrivia },
			new CSharpTokenData { EndIndex = 141, StartIndex = 121, Type = SyntaxKind.SingleLineCommentTrivia },
			new CSharpTokenData { EndIndex = 143, StartIndex = 141, Type = SyntaxKind.EndOfLineTrivia },
			new CSharpTokenData { EndIndex = 144, StartIndex = 143, Type = SyntaxKind.CloseBraceToken }
			// </Scenarios>
		]);

		return response.ToArray();
	}

	#endregion
}