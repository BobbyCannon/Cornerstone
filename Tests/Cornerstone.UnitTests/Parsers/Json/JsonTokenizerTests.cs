#region References

using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Json;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Json;

[TestClass]
public class JsonTokenizerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ArrayMixedTypes()
	{
		Process(
			"""
			[1,"foo",true,null]
			""",
			new Token { SyntaxKind = SyntaxKind.None, EndOffset = 1, StartOffset = 0, Type = JsonTokenizer.TokenTypeLeftBracket },
			new Token { SyntaxKind = SyntaxKind.Number, EndOffset = 2, StartOffset = 1, Type = JsonTokenizer.TokenTypeNumber },
			new Token { SyntaxKind = SyntaxKind.None, EndOffset = 3, StartOffset = 2, Type = JsonTokenizer.TokenTypeComma },
			new Token { SyntaxKind = SyntaxKind.String, EndOffset = 8, StartOffset = 3, Type = JsonTokenizer.TokenTypeString },
			new Token { SyntaxKind = SyntaxKind.None, EndOffset = 9, StartOffset = 8, Type = JsonTokenizer.TokenTypeComma },
			new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 13, StartOffset = 9, Type = JsonTokenizer.TokenTypeTrue },
			new Token { SyntaxKind = SyntaxKind.None, EndOffset = 14, StartOffset = 13, Type = JsonTokenizer.TokenTypeComma },
			new Token { SyntaxKind = SyntaxKind.Keyword, EndOffset = 18, StartOffset = 14, Type = JsonTokenizer.TokenTypeNull },
			new Token { SyntaxKind = SyntaxKind.None, EndOffset = 19, StartOffset = 18, Type = JsonTokenizer.TokenTypeRightBracket }
		);
	}

	[TestMethod]
	public void BasicTypes()
	{
		Process("1", new Token(JsonTokenizer.TokenTypeNumber, 0, 1, SyntaxKind.Number, false, false, false));
		Process("1.23", new Token(JsonTokenizer.TokenTypeNumber, 0, 4, SyntaxKind.Number, false, false, false));
		Process("0.987", new Token(JsonTokenizer.TokenTypeNumber, 0, 5, SyntaxKind.Number, false, false, false));
		Process("true", new Token(JsonTokenizer.TokenTypeTrue, 0, 4, SyntaxKind.Keyword, false, false, false));
		Process("false", new Token(JsonTokenizer.TokenTypeFalse, 0, 5, SyntaxKind.Keyword, false, false, false));
		Process("null", new Token(JsonTokenizer.TokenTypeNull, 0, 4, SyntaxKind.Keyword, false, false, false));
		Process("\t", new Token(TextProcessor.TokenTypeWhitespace, 0, 1, SyntaxKind.None, false, false, false));
		Process(" ", new Token(TextProcessor.TokenTypeWhitespace, 0, 1, SyntaxKind.None, false, false, false));
		Process(" \t ", new Token(TextProcessor.TokenTypeWhitespace, 0, 3, SyntaxKind.None, false, false, false));
		Process("\"foo bar\"", new Token(JsonTokenizer.TokenTypeString, 0, 9, SyntaxKind.String, false, false, false));
		Process("\"foo bar", new Token(JsonTokenizer.TokenTypeString, 0, 8, SyntaxKind.String, false, false, false));
		Process("foo", new Token(TextProcessor.TokenTypeError, 0, 3, SyntaxKind.Error, false, false, false));
		Process(" bar\t",
			new Token(TextProcessor.TokenTypeWhitespace, 0, 1, SyntaxKind.None, false, false, false),
			new Token(TextProcessor.TokenTypeError, 1, 4, SyntaxKind.Error, false, false, false),
			new Token(TextProcessor.TokenTypeWhitespace, 4, 5, SyntaxKind.None, false, false, false)
		);
	}

	[TestMethod]
	public void Empty()
	{
		Process("");

		Process("{}",
			new Token(JsonTokenizer.TokenTypeLeftBrace, 0, 1, SyntaxKind.None, false, false, false),
			new Token(JsonTokenizer.TokenTypeRightBrace, 1, 2, SyntaxKind.None, false, false, false)
		);

		Process("[]",
			new Token(JsonTokenizer.TokenTypeLeftBracket, 0, 1, SyntaxKind.None, false, false, false),
			new Token(JsonTokenizer.TokenTypeRightBracket, 1, 2, SyntaxKind.None, false, false, false)
		);
	}

	private void Process(string json, params Token[] expected)
	{
		var buffer = new StringGapBuffer(json);
		var pool = new SpeedyQueue<Token>();
		var tokenizer = new JsonTokenizer(buffer, pool);
		var tokens = tokenizer.Process().ToArray();
		tokens.DumpCSharp();
		AreEqual(expected, tokens);
	}

	#endregion
}