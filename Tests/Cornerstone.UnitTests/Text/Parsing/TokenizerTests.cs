#region References

using System;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Text;
using Cornerstone.Text.Parsing;
using Cornerstone.Text.Parsing.CSharp;
using Cornerstone.Text.Parsing.Json;
using Cornerstone.Text.Parsing.Markdown;
using Cornerstone.Text.Parsing.PowerShell;
using Cornerstone.Text.Parsing.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text.Parsing;

[TestClass]
public class TokenizerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CalculateUntilNot()
	{
		//                                01234567890
		var buffer = new StringGapBuffer("#### Header");
		var pool = new SpeedyQueue<Token>();
		var tokenizer = new Tokenizer(buffer, pool);
		AreEqual(4, tokenizer.CalculateUntilNot(1, '#'));
		AreEqual(4, tokenizer.CalculateUntilNot(3, '#'));
		AreEqual(4, tokenizer.CalculateUntilNot(4, '#'));
	}

	[TestMethod]
	public void ConsumeRestOfLine()
	{
		//                                012345678901 2 3456 7 8901234
		var buffer = new StringGapBuffer("#### Header\r\n---\r\n1. Test");
		var pool = new SpeedyQueue<Token>();
		var tokenizer = new Tokenizer(buffer, pool);
		tokenizer.ConsumeRestOfLine();
		AreEqual(11, tokenizer.Position);
		AreEqual(13, tokenizer.ConsumeWhitespace());
		tokenizer.ConsumeRestOfLine();
		AreEqual(16, tokenizer.Position);
	}

	[TestMethod]
	public void GetByExtensionAcceptsDottedAndBareNames()
	{
		var buffer = new StringGapBuffer("class Foo { }");
		var pool = new SpeedyQueue<Token>();

		IsTrue(Tokenizer.GetByExtension("cs", buffer, pool) is CSharpTokenizer);
		IsTrue(Tokenizer.GetByExtension(".cs", buffer, pool) is CSharpTokenizer);
		IsTrue(Tokenizer.GetByExtension("CS", buffer, pool) is CSharpTokenizer);
		IsTrue(Tokenizer.GetByExtension(".json", buffer, pool) is JsonTokenizer);
		IsTrue(Tokenizer.GetByExtension("md", buffer, pool) is MarkdownTokenizer);
		IsTrue(Tokenizer.GetByExtension(".cxaml", buffer, pool) is XmlTokenizer);
		IsTrue(Tokenizer.GetByExtension(".axaml", buffer, pool) is XmlTokenizer);
		IsTrue(Tokenizer.GetByExtension("html", buffer, pool) is XmlTokenizer);
		IsTrue(Tokenizer.GetByExtension(".htm", buffer, pool) is XmlTokenizer);
		IsTrue(Tokenizer.GetByExtension("ps1", buffer, pool) is PowerShellTokenizer);
		IsTrue(Tokenizer.GetByExtension(".psm1", buffer, pool) is PowerShellTokenizer);
		IsNull(Tokenizer.GetByExtension(string.Empty, buffer, pool));
		IsNull(Tokenizer.GetByExtension("js", buffer, pool));
	}

	[TestMethod]
	public void NextSectionSkipsOneCharacterWhenTokenizerThrows()
	{
		var buffer = new StringGapBuffer("ab");
		var tokenizer = new ThrowingTokenizer(buffer, new SpeedyQueue<Token>());
		var tokens = tokenizer.Process().ToArray();
		AreEqual(2, tokens.Length);
		AreEqual(TextProcessor.TokenTypeText, tokens[0].Type);
		AreEqual(0, tokens[0].StartOffset);
		AreEqual(1, tokens[0].EndOffset);
		AreEqual(1, tokens[1].StartOffset);
		AreEqual(2, tokens[1].EndOffset);
	}

	[TestMethod]
	public void PowerShellTokenizerHighlightsCommonConstructs()
	{
		var buffer = new StringGapBuffer(
			"# comment\r\nfunction Get-Thing {\r\n\tparam($Name)\r\n\tif ($Name -eq \"x\") { Get-Item -Path C:\\Temp }\r\n\tWrite-Host \"Hi $Name\"\r\n\t$null = 1kb\r\n}\r\n");
		var pool = new SpeedyQueue<Token>();
		var tokenizer = new PowerShellTokenizer(buffer, pool);

		var tokens = tokenizer.Process().ToList();
		IsTrue(tokens.Count > 0);
		IsTrue(tokens.Any(t => t.Type == PowerShellTokenizer.TokenTypeCommentInline));
		IsTrue(tokens.Any(t => t.Type == PowerShellTokenizer.TokenTypeKeyword));
		IsTrue(tokens.Any(t => t.Type == PowerShellTokenizer.TokenTypeVariable));
		IsTrue(tokens.Any(t => t.Type == PowerShellTokenizer.TokenTypeString));
		IsTrue(tokens.Any(t => t.Type == PowerShellTokenizer.TokenTypeNumber));
		IsTrue(tokens.Any(t => t.Type == PowerShellTokenizer.TokenTypeCommand));
		IsTrue(tokens.Any(t => t.Type == PowerShellTokenizer.TokenTypeOperator));
		IsTrue(tokens.Any(t => t.Type == PowerShellTokenizer.TokenTypeParameter));
	}

	[TestMethod]
	public void PowerShellTokenizerTreatsSingleLetterDashFlagsAsParameters()
	{
		var source = "dotnet publish -f net10.0-ios -c Release -p:RuntimeIdentifier=ios-arm64";
		var buffer = new StringGapBuffer(source);
		var pool = new SpeedyQueue<Token>();
		var tokenizer = new PowerShellTokenizer(buffer, pool);
		var tokens = tokenizer.Process().ToList();

		string TextOf(Token t)
		{
			return source.Substring(t.StartOffset, t.Length);
		}

		IsTrue(tokens.Any(t => (t.Type == PowerShellTokenizer.TokenTypeParameter) && (TextOf(t) == "-f")));
		IsTrue(tokens.Any(t => (t.Type == PowerShellTokenizer.TokenTypeParameter) && (TextOf(t) == "-c")));
		IsTrue(tokens.Any(t => (t.Type == PowerShellTokenizer.TokenTypeParameter) && (TextOf(t) == "-p:RuntimeIdentifier")));
		IsFalse(tokens.Any(t => t.Type == PowerShellTokenizer.TokenTypeOperator));
	}

	#endregion

	#region Classes

	private sealed class ThrowingTokenizer : Tokenizer
	{
		#region Constructors

		public ThrowingTokenizer(IStringBuffer buffer, IQueue<Token> pool)
			: base(buffer, pool)
		{
		}

		#endregion

		#region Methods

		protected override bool TryProcessPosition(out Token token)
		{
			throw new InvalidOperationException("tokenizer fault");
		}

		#endregion
	}

	#endregion
}