#region References

using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Xml;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Xml;

[TestClass]
public class XmlTokenizerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void BasicExample()
	{
		Process(
			"<!-- Comment -->",
			new Token { Color = SyntaxColor.Comment, EndOffset = 16, StartOffset = 0, Type = XmlTokenizer.TokenTypeComment }
		);

		Process(
			"<![CDATA[\r\nThis product <b>must</b> be stored at 5°C & handled with care\r\n]]>",
			new Token { Color = SyntaxColor.Comment, EndOffset = 77, StartOffset = 0, Type = XmlTokenizer.TokenTypeCData }
		);

		Process(
			"<!ELEMENT note (to, from, heading, body)>",
			new Token { Color = SyntaxColor.None, EndOffset = 41, StartOffset = 0, Type = XmlTokenizer.TokenTypeDocType }
		);

		Process(
			"""
			<Person Name="John" Age="21" />
			""",
			new Token { Color = SyntaxColor.Preprocessor, EndOffset = 1, StartOffset = 0, Type = XmlTokenizer.TokenTypeStartTagOpen },
			new Token { Color = SyntaxColor.Keyword, EndOffset = 7, StartOffset = 1, Type = XmlTokenizer.TokenTypeTagName },
			new Token { Color = SyntaxColor.None, EndOffset = 8, StartOffset = 7, Type = Tokenizer.TokenTypeWhitespace },
			new Token { Color = SyntaxColor.Variable, EndOffset = 12, StartOffset = 8, Type = XmlTokenizer.TokenTypeAttributeName },
			new Token { Color = SyntaxColor.None, EndOffset = 13, StartOffset = 12, Type = XmlTokenizer.TokenTypeEquals },
			new Token { Color = SyntaxColor.String, EndOffset = 19, StartOffset = 13, Type = XmlTokenizer.TokenTypeAttributeValue },
			new Token { Color = SyntaxColor.None, EndOffset = 20, StartOffset = 19, Type = Tokenizer.TokenTypeWhitespace },
			new Token { Color = SyntaxColor.Variable, EndOffset = 23, StartOffset = 20, Type = XmlTokenizer.TokenTypeAttributeName },
			new Token { Color = SyntaxColor.None, EndOffset = 24, StartOffset = 23, Type = XmlTokenizer.TokenTypeEquals },
			new Token { Color = SyntaxColor.String, EndOffset = 28, StartOffset = 24, Type = XmlTokenizer.TokenTypeAttributeValue },
			new Token { Color = SyntaxColor.None, EndOffset = 29, StartOffset = 28, Type = Tokenizer.TokenTypeWhitespace },
			new Token { Color = SyntaxColor.None, EndOffset = 31, StartOffset = 29, Type = XmlTokenizer.TokenTypeEmptyElementClose }
		);
	}

	[TestMethod]
	public void MultilineWithSpaces()
	{
		Process(
			"""
			<whitespace-test    
			    attr-with-spaces   =   "value"   
			    single='value'
			/>
			""",
			new Token { Color = SyntaxColor.Preprocessor, EndOffset = 1, StartOffset = 0, Type = XmlTokenizer.TokenTypeStartTagOpen },
			new Token { Color = SyntaxColor.Keyword, EndOffset = 16, StartOffset = 1, Type = XmlTokenizer.TokenTypeTagName },
			new Token { Color = SyntaxColor.None, EndOffset = 26, StartOffset = 16, Type = Tokenizer.TokenTypeWhitespace },
			new Token { Color = SyntaxColor.Variable, EndOffset = 42, StartOffset = 26, Type = XmlTokenizer.TokenTypeAttributeName },
			new Token { Color = SyntaxColor.None, EndOffset = 45, StartOffset = 42, Type = Tokenizer.TokenTypeWhitespace },
			new Token { Color = SyntaxColor.None, EndOffset = 46, StartOffset = 45, Type = XmlTokenizer.TokenTypeEquals },
			new Token { Color = SyntaxColor.None, EndOffset = 49, StartOffset = 46, Type = Tokenizer.TokenTypeWhitespace },
			new Token { Color = SyntaxColor.String, EndOffset = 56, StartOffset = 49, Type = XmlTokenizer.TokenTypeAttributeValue },
			new Token { Color = SyntaxColor.None, EndOffset = 65, StartOffset = 56, Type = Tokenizer.TokenTypeWhitespace },
			new Token { Color = SyntaxColor.Variable, EndOffset = 71, StartOffset = 65, Type = XmlTokenizer.TokenTypeAttributeName },
			new Token { Color = SyntaxColor.None, EndOffset = 72, StartOffset = 71, Type = XmlTokenizer.TokenTypeEquals },
			new Token { Color = SyntaxColor.String, EndOffset = 79, StartOffset = 72, Type = XmlTokenizer.TokenTypeAttributeValue },
			new Token { Color = SyntaxColor.None, EndOffset = 81, StartOffset = 79, Type = Tokenizer.TokenTypeWhitespace },
			new Token { Color = SyntaxColor.None, EndOffset = 83, StartOffset = 81, Type = XmlTokenizer.TokenTypeEmptyElementClose }
		);
	}

	private void Process(string xml, params Token[] expected)
	{
		var buffer = new StringGapBuffer(xml);
		var pool = new SpeedyQueue<Token>();
		var tokenizer = new XmlTokenizer(buffer, pool);
		var tokens = tokenizer.Process().ToArray();
		AreEqual(expected, tokens, () =>
		{
			tokens.DumpCSharp();
			return xml;
		});
	}

	#endregion
}