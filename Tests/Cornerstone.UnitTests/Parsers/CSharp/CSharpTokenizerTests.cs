#region References

using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Parsers.CSharp;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.CSharp;

[TestClass]
public class CSharpTokenizerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Attributes()
	{
		Process("[JsonIgnore]",
			new Token { Color = SyntaxColor.Attribute, EndOffset = 12, StartOffset = 0, Type = CSharpTokenizer.TokenTypeAttribute }
		);
		Process("[Pack(1,2)]",
			new Token { Color = SyntaxColor.Attribute, EndOffset = 11, StartOffset = 0, Type = CSharpTokenizer.TokenTypeAttribute }
		);
	}
	
	[TestMethod]
	public void Comments()
	{
		Process("// Hello World",
			new Token { Color = SyntaxColor.Comment, EndOffset = 14, StartOffset = 0, Type = CSharpTokenizer.TokenTypeCommentInline }
		);
		Process("/* Hello\nWorld */",
			new Token { Color = SyntaxColor.Comment, EndOffset = 17, StartOffset = 0, Type = CSharpTokenizer.TokenTypeCommentMultiline }
		);
		Process("""
				/// <summary>
				/// This is a test.
				/// </summary>
				""",
			new Token { Color = SyntaxColor.Comment, EndOffset = 50, StartOffset = 0, Type = CSharpTokenizer.TokenTypeCommentXml }
		);
	}

	private void Process(string json, params Token[] expected)
	{
		var buffer = new StringGapBuffer(json);
		var pool = new SpeedyQueue<Token>();
		var tokenizer = new CSharpTokenizer(buffer, pool);
		var tokens = tokenizer.Process().ToArray();
		tokens.DumpCSharp();
		AreEqual(expected, tokens);
	}

	#endregion
}