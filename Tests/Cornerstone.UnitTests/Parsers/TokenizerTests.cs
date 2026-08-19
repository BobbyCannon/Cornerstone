#region References

using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Parsers.CSharp;
using Cornerstone.Parsers.Json;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Parsers.Xml;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers;

[TestClass]
public class TokenizerTests : CornerstoneUnitTest
{
	#region Methods

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
		IsTrue(Tokenizer.GetByExtension(".axaml", buffer, pool) is XmlTokenizer);
		IsNull(Tokenizer.GetByExtension(string.Empty, buffer, pool));
		IsNull(Tokenizer.GetByExtension("js", buffer, pool));
	}

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

	#endregion
}