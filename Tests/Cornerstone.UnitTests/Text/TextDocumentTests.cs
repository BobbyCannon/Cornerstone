#region References

using System.Linq;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class TextDocumentTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void FindAnyCharacter()
	{
		//             01234567890123456789012
		var content = "[Test](http://test.com)";
		var document = TextDocument.Load(content);

		IsTrue(document.FindAnyCharacter(0, 2, ['['], out var actual) && (actual == 0));
		IsTrue(document.FindAnyCharacter(0, 2, ['T'], out actual) && (actual == 1));
		IsTrue(document.FindAnyCharacter(7, 11, ['h'], out actual) && (actual == 7));
		IsTrue(document.FindAnyCharacter(7, 11, ['t'], out actual) && (actual == 8));
		IsTrue(document.FindAnyCharacter(7, 11, ['p'], out actual) && (actual == 10));
		IsTrue(document.FindAnyCharacter(0, 23, [')'], out actual) && (actual == 22));

		// Bad end index should not exception
		IsTrue(!document.FindAnyCharacter(0, 100, ['+'], out actual) && (actual == -1));

		// Should not match because 2 is exclusive
		IsTrue(!document.FindAnyCharacter(0, 2, ['e'], out actual) && (actual == -1));

		// t will not match T
		IsTrue(!document.FindAnyCharacter(0, 2, ['t'], out actual) && (actual == -1));
	}

	[TestMethod]
	public void FindAnyCharacterExceptInReverse()
	{
		//             01234567890123456789012
		var content = "[Test](http://test.com)";
		var document = TextDocument.Load(content);

		IsTrue(document.FindAnyCharacterExceptInReverse(0, 5, ['[', ']'], out var actual) && (actual == 4));
		IsTrue(document.FindAnyCharacterExceptInReverse(0, 22, [' '], out actual) && (actual == 22));
	}

	[TestMethod]
	public void FindAnyCharacterInReverse()
	{
		//             01234567890123456789012
		var content = "[Test](http://test.com)";
		var document = TextDocument.Load(content);

		IsTrue(document.FindAnyCharacterInReverse(0, 2, ['['], out var actual) && (actual == 0));
		IsTrue(document.FindAnyCharacterInReverse(0, 2, ['T'], out actual) && (actual == 1));
		IsTrue(document.FindAnyCharacterInReverse(7, 11, ['h'], out actual) && (actual == 7));
		IsTrue(document.FindAnyCharacterInReverse(7, 11, ['t'], out actual) && (actual == 9));
		IsTrue(document.FindAnyCharacterInReverse(7, 11, ['p'], out actual) && (actual == 10));
		IsTrue(document.FindAnyCharacterInReverse(0, 23, [')'], out actual) && (actual == 22));
		IsTrue(document.FindAnyCharacterInReverse(-2, 2, ['e'], out actual) && (actual == 2));

		// Bad indexes should not exception
		IsTrue(!document.FindAnyCharacterInReverse(-10, 100, ['+'], out actual) && (actual == -1));

		// t will not match T
		IsTrue(!document.FindAnyCharacterInReverse(-3, 2, ['t'], out actual) && (actual == -1));
	}

	[TestMethod]
	public void FindCharactersIndexes()
	{
		//             0123456789012345678901234
		var content = "## { Red } Header\r\nMore";
		var document = TextDocument.Load(content);
		var actual = document.FindCharactersIndexes(0, ['#', '{', '}'], [' '], ['\r', '\n', '\0']);
		var expected = new[] { 0, 3, 9, 16 };
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void FindCharactersPattern()
	{
		var content = "[Test](http://test.com)";
		var document = TextDocument.Load(content);
		var actual = document.FindCharactersPattern(0, ['[', ']', '(', ')'], ['\r', '\n', '\0']);
		var expected = new[] { 0, 5, 6, 22 };
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void MatchStrings()
	{
		var content = """
					```Csharp
					public void Test()
					{
					}
					```
					""";

		var document = TextDocument.Load(content);
		var actual = document.MatchStrings(0, ["```", "```"]);
		var expected = new[] { 0, 37 };
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void MoveReadIndex()
	{
		var content = "0123456789";
		var reader = TextDocument.Load(content);
		AreEqual(10, reader.ReadIndex);

		reader.Move(-8);
		AreEqual(2, reader.ReadIndex);

		reader.Move(5);
		AreEqual(7, reader.ReadIndex);

		reader.Move(int.MinValue);
		AreEqual(0, reader.ReadIndex);

		reader.Move(int.MaxValue);
		AreEqual(10, reader.ReadIndex);
	}

	[TestMethod]
	public void MoveToReadIndex()
	{
		var content = "0123456789";
		var reader = TextDocument.Load(content);
		AreEqual(10, reader.ReadIndex);

		reader.MoveTo(10);
		AreEqual(10, reader.ReadIndex);

		reader.MoveTo(5);
		AreEqual(5, reader.ReadIndex);

		reader.MoveTo(0);
		AreEqual(0, reader.ReadIndex);

		reader.MoveTo(int.MaxValue);
		AreEqual(10, reader.ReadIndex);

		reader.MoveTo(int.MinValue);
		AreEqual(0, reader.ReadIndex);
	}

	[TestMethod]
	public void ReadLines()
	{
		//             01 2 34 567 890 1 2
		var content = "a\r\nb\nc1\rc2\n\rd";
		var reader = TextDocument.Load(content);

		AreEqual(13, reader.ReadIndex);
		AreEqual(1, reader.ColumnNumber);
		AreEqual(5, reader.LineNumber);
		AreEqual(13, reader.Length);

		var actual = reader.Lines;
		var expected = new TextRangeData[]
		{
			new() { EndIndex = 3, Length = 3, Remaining = 10, StartIndex = 0 },
			new() { EndIndex = 5, Length = 2, Remaining = 8, StartIndex = 3 },
			new() { EndIndex = 11, Length = 6, Remaining = 2, StartIndex = 5 },
			new() { EndIndex = 13, Length = 2, Remaining = 0, StartIndex = 11 }
		};

		AreEqual(expected, actual);

		var expectedValues = new[] { "a\r\n", "b\n", "c1\rc2\n", "\rd" };
		var actualValues = actual.Select(x => x.ToString());

		AreEqual(expectedValues, actualValues);
	}

	[TestMethod]
	public void ReadLinesUsingCRLF()
	{
		var content = "foo\r\nbar\r\nhello\r\nworld";
		var options = new TextDocumentSettings { EndOfLineCharacters = "\r\n" };
		var reader = TextDocument.Load(content, options);

		AreEqual(22, reader.ReadIndex);
		AreEqual(1, reader.ColumnNumber);
		AreEqual(5, reader.LineNumber);
		AreEqual(22, reader.Length);

		var actual = reader.Lines;
		var expected = new TextRangeData[]
		{
			new() { EndIndex = 5, Length = 5, Remaining = 17, StartIndex = 0 },
			new() { EndIndex = 10, Length = 5, Remaining = 12, StartIndex = 5 },
			new() { EndIndex = 17, Length = 7, Remaining = 5, StartIndex = 10 },
			new() { EndIndex = 22, Length = 5, Remaining = 0, StartIndex = 17 }
		};

		AreEqual(expected, actual, () => actual.DumpJson());

		var expectedValues = new[] { "foo\r\n", "bar\r\n", "hello\r\n", "world" };
		var actualValues = actual.Select(x => x.ToString());

		AreEqual(expectedValues, actualValues);
	}

	[TestMethod]
	public void ReadLinesUsingLF()
	{
		var content = "foo\nbar\nhello\nworld";
		var options = new TextDocumentSettings { EndOfLineCharacters = "\n" };
		var reader = TextDocument.Load(content, options);

		AreEqual(19, reader.ReadIndex);
		AreEqual(1, reader.ColumnNumber);
		AreEqual(5, reader.LineNumber);
		AreEqual(19, reader.Length);

		var actual = reader.Lines;
		var expected = new TextRangeData[]
		{
			new() { EndIndex = 4, Length = 4, Remaining = 15, StartIndex = 0 },
			new() { EndIndex = 8, Length = 4, Remaining = 11, StartIndex = 4 },
			new() { EndIndex = 14, Length = 6, Remaining = 5, StartIndex = 8 },
			new() { EndIndex = 19, Length = 5, Remaining = 0, StartIndex = 14 }
		};

		AreEqual(expected, actual, () => actual.DumpJson());

		var expectedValues = new[] { "foo\n", "bar\n", "hello\n", "world" };
		var actualValues = actual.Select(x => x.ToString());

		AreEqual(expectedValues, actualValues);
	}

	[TestMethod]
	public void SubStringUsing()
	{
		//             01234567890123456789012
		var content = "[Test](http://test.com)";
		var document = TextDocument.Load(content);

		AreEqual("[Test]", document.SubStringUsingAbsoluteIndexes(0, 5, true));
		AreEqual("Test", document.SubStringUsingAbsoluteIndexes(0, 5, false));
	}

	#endregion
}