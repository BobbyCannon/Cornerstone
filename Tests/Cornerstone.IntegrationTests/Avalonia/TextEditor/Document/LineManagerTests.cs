#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Text.Document;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework.Legacy;
using NUnitAssert = NUnit.Framework.Assert;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Document;

[TestClass]
public class LineManagerTests : CornerstoneUnitTest
{
	#region Fields

	private TextEditorDocument _document;

	#endregion

	#region Methods

	[TestMethod]
	public void CheckClearingDocument()
	{
		_document.Text = "Hello,\nWorld!";
		AreEqual(2, _document.LineCount);
		var oldLines = _document.Lines.ToArray();
		_document.Text = "";
		AreEqual("", _document.Text);
		AreEqual(0, _document.TextLength);
		AreEqual(1, _document.LineCount);
		ClassicAssert.AreSame(oldLines[0], _document.Lines.Single());
		IsFalse(oldLines[0].IsDeleted);
		IsTrue(oldLines[1].IsDeleted);
		ClassicAssert.IsNull(oldLines[0].NextLine);
		ClassicAssert.IsNull(oldLines[1].PreviousLine);
	}

	[TestMethod]
	public void CheckEmptyDocument()
	{
		AreEqual("", _document.Text);
		AreEqual(0, _document.TextLength);
		AreEqual(1, _document.LineCount);
	}

	[TestMethod]
	public void CheckGetLineInEmptyDocument()
	{
		AreEqual(1, _document.Lines.Count);
		var lines = new List<DocumentLine>(_document.Lines);
		AreEqual(1, lines.Count);
		var line = _document.Lines[0];
		ClassicAssert.AreSame(line, lines[0]);
		ClassicAssert.AreSame(line, _document.GetLineByNumber(1));
		ClassicAssert.AreSame(line, _document.GetLineByOffset(0));
	}

	[TestMethod]
	public void CheckLineSegmentInEmptyDocument()
	{
		var line = _document.GetLineByNumber(1);
		AreEqual(1, line.LineNumber);
		AreEqual(0, line.StartIndex);
		IsFalse(line.IsDeleted);
		AreEqual(0, line.Length);
		AreEqual(0, line.TotalLength);
		AreEqual(0, line.DelimiterLength);
	}

	[TestMethod]
	public void CheckMixedNewLineTest()
	{
		const string mixedNewlineText = "line 1\nline 2\r\nline 3\rline 4";
		_document.Text = mixedNewlineText;
		AreEqual(mixedNewlineText, _document.Text);
		AreEqual(4, _document.LineCount);
		for (var i = 1; i < 4; i++)
		{
			var line = _document.GetLineByNumber(i);
			AreEqual(i, line.LineNumber);
			AreEqual("line " + i, _document.GetText(line));
		}
		AreEqual(1, _document.GetLineByNumber(1).DelimiterLength);
		AreEqual(2, _document.GetLineByNumber(2).DelimiterLength);
		AreEqual(1, _document.GetLineByNumber(3).DelimiterLength);
		AreEqual(0, _document.GetLineByNumber(4).DelimiterLength);
	}

	[TestMethod]
	public void ExtendDelimiter1()
	{
		_document.Text = "a\nc";
		_document.Insert(1, "b\r");
		AreEqual("ab\r\nc", _document.Text);
		CheckDocumentLines("ab",
			"c");
	}

	[TestMethod]
	public void ExtendDelimiter2()
	{
		_document.Text = "a\rc";
		_document.Insert(2, "\nb");
		AreEqual("a\r\nbc", _document.Text);
		CheckDocumentLines("a",
			"bc");
	}

	[TestMethod]
	public void FixUpFirstPartOfDelimiter()
	{
		_document.Text = "a\n\nb";
		_document.Replace(1, 1, "\r");
		AreEqual("a\r\nb", _document.Text);
		CheckDocumentLines("a",
			"b");
	}

	[TestMethod]
	public void FixUpSecondPartOfDelimiter()
	{
		_document.Text = "a\r\rb";
		_document.Replace(2, 1, "\n");
		AreEqual("a\r\nb", _document.Text);
		CheckDocumentLines("a",
			"b");
	}

	[TestMethod]
	public void GetCharAt()
	{
		_document.Text = "a\r\nb";
		AreEqual('a', _document.GetCharAt(0));
		AreEqual('\r', _document.GetCharAt(1));
		AreEqual('\n', _document.GetCharAt(2));
		AreEqual('b', _document.GetCharAt(3));
	}

	[TestMethod]
	public void GetCharAt0EmptyDocument()
	{
		NUnitAssert.Throws<IndexOutOfRangeException>(() => _document.GetCharAt(0));
	}

	[TestMethod]
	public void GetCharAtEndOffset()
	{
		NUnitAssert.Throws<IndexOutOfRangeException>(() =>
		{
			_document.Text = "a\nb";
			_document.GetCharAt(_document.TextLength);
		});
	}

	[TestMethod]
	public void GetCharAtNegativeOffset()
	{
		NUnitAssert.Throws<IndexOutOfRangeException>(() =>
		{
			_document.Text = "a\nb";
			_document.GetCharAt(-1);
		});
	}

	[TestMethod]
	public void GetLineByNumberNegative()
	{
		NUnitAssert.Throws<ArgumentOutOfRangeException>(() =>
		{
			_document.Text = "a\nb";
			_document.GetLineByNumber(-1);
		});
	}

	[TestMethod]
	public void GetLineByNumberTooHigh()
	{
		NUnitAssert.Throws<ArgumentOutOfRangeException>(() =>
		{
			_document.Text = "a\nb";
			_document.GetLineByNumber(3);
		});
	}

	[TestMethod]
	public void GetLineByOffsetNegative()
	{
		NUnitAssert.Throws<ArgumentOutOfRangeException>(() =>
		{
			_document.Text = "a\nb";
			_document.GetLineByOffset(-1);
		});
	}

	[TestMethod]
	public void GetLineByOffsetToHigh()
	{
		NUnitAssert.Throws<ArgumentOutOfRangeException>(() =>
		{
			_document.Text = "a\nb";
			_document.GetLineByOffset(10);
		});
	}

	[TestMethod]
	public void GetOffset()
	{
		_document.Text = "Hello,\nWorld!";
		AreEqual(0, _document.GetOffset(1, 1));
		AreEqual(1, _document.GetOffset(1, 2));
		AreEqual(5, _document.GetOffset(1, 6));
		AreEqual(6, _document.GetOffset(1, 7));
		AreEqual(7, _document.GetOffset(2, 1));
		AreEqual(8, _document.GetOffset(2, 2));
		AreEqual(12, _document.GetOffset(2, 6));
		AreEqual(13, _document.GetOffset(2, 7));
	}

	[TestMethod]
	public void GetOffsetIgnoreNegativeColumns()
	{
		_document.Text = "Hello,\nWorld!";
		AreEqual(0, _document.GetOffset(1, -1));
		AreEqual(0, _document.GetOffset(1, -100));
		AreEqual(0, _document.GetOffset(1, 0));
		AreEqual(7, _document.GetOffset(2, -1));
		AreEqual(7, _document.GetOffset(2, -100));
		AreEqual(7, _document.GetOffset(2, 0));
	}

	[TestMethod]
	public void GetOffsetIgnoreTooHighColumns()
	{
		_document.Text = "Hello,\nWorld!";
		AreEqual(6, _document.GetOffset(1, 8));
		AreEqual(6, _document.GetOffset(1, 100));
		AreEqual(13, _document.GetOffset(2, 8));
		AreEqual(13, _document.GetOffset(2, 100));
	}

	[TestMethod]
	public void InsertAfterEndOffset()
	{
		NUnitAssert.Throws<ArgumentOutOfRangeException>(() =>
		{
			_document.Text = "a\nb";
			_document.Insert(4, "text");
		});
	}

	[TestMethod]
	public void InsertAtEndOffset()
	{
		_document.Text = "a\nb";
		CheckDocumentLines("a",
			"b");
		_document.Insert(3, "text");
		CheckDocumentLines("a",
			"btext");
	}

	[TestMethod]
	public void InsertAtNegativeOffset()
	{
		NUnitAssert.Throws<ArgumentOutOfRangeException>(() =>
		{
			_document.Text = "a\nb";
			_document.Insert(-1, "text");
		});
	}

	[TestMethod]
	public void InsertInEmptyDocument()
	{
		_document.Insert(0, "a");
		AreEqual(_document.LineCount, 1);
		var line = _document.GetLineByNumber(1);
		AreEqual("a", _document.GetText(line));
	}

	[TestMethod]
	public void InsertInsideDelimiter()
	{
		_document.Text = "a\r\nc";
		_document.Insert(2, "b");
		AreEqual("a\rb\nc", _document.Text);
		CheckDocumentLines("a",
			"b",
			"c");
	}

	[TestMethod]
	public void InsertInsideDelimiter2()
	{
		_document.Text = "a\r\nd";
		_document.Insert(2, "b\nc");
		AreEqual("a\rb\nc\nd", _document.Text);
		CheckDocumentLines("a",
			"b",
			"c",
			"d");
	}

	[TestMethod]
	public void InsertInsideDelimiter3()
	{
		_document.Text = "a\r\nc";
		_document.Insert(2, "b\r");
		AreEqual("a\rb\r\nc", _document.Text);
		CheckDocumentLines("a",
			"b",
			"c");
	}

	[TestMethod]
	public void InsertNothing()
	{
		_document.Insert(0, "");
		AreEqual(_document.LineCount, 1);
		AreEqual(_document.TextLength, 0);
	}

	[TestMethod]
	public void InsertNull()
	{
		// Should not throw, should just not insert.
		_document.Insert(0, (string) null);
	}

	[TestMethod]
	public void LfCrIsTwoNewLinesTest()
	{
		_document.Text = "a\n\rb";
		AreEqual("a\n\rb", _document.Text);
		CheckDocumentLines("a",
			"",
			"b");
	}

	[TestMethod]
	public void LineIndexOfTest()
	{
		var line = _document.GetLineByNumber(1);
		AreEqual(0, _document.Lines.IndexOf(line));
		var lineFromOtherDocument = new TextEditorDocument().GetLineByNumber(1);
		AreEqual(-1, _document.Lines.IndexOf(lineFromOtherDocument));
		_document.Text = "a\nb\nc";
		var middleLine = _document.GetLineByNumber(2);
		AreEqual(1, _document.Lines.IndexOf(middleLine));
		_document.Remove(1, 3);
		IsTrue(middleLine.IsDeleted);
		AreEqual(-1, _document.Lines.IndexOf(middleLine));
	}

	[TestMethod]
	public void RemoveFirstPartOfDelimiter()
	{
		_document.Text = "a\r\nb";
		_document.Remove(1, 1);
		AreEqual("a\nb", _document.Text);
		CheckDocumentLines("a",
			"b");
	}

	[TestMethod]
	public void RemoveFromSecondPartOfDelimiter()
	{
		_document.Text = "a\r\nb\nc";
		_document.Remove(2, 3);
		AreEqual("a\rc", _document.Text);
		CheckDocumentLines("a",
			"c");
	}

	[TestMethod]
	public void RemoveFromSecondPartOfDelimiterToDocumentEnd()
	{
		_document.Text = "a\r\nb";
		_document.Remove(2, 2);
		AreEqual("a\r", _document.Text);
		CheckDocumentLines("a",
			"");
	}

	[TestMethod]
	public void RemoveLineContentAndJoinDelimiters()
	{
		_document.Text = "a\rb\nc";
		_document.Remove(2, 1);
		AreEqual("a\r\nc", _document.Text);
		CheckDocumentLines("a",
			"c");
	}

	[TestMethod]
	public void RemoveLineContentAndJoinDelimiters2()
	{
		_document.Text = "a\rb\nc\nd";
		_document.Remove(2, 3);
		AreEqual("a\r\nd", _document.Text);
		CheckDocumentLines("a",
			"d");
	}

	[TestMethod]
	public void RemoveLineContentAndJoinDelimiters3()
	{
		_document.Text = "a\rb\r\nc";
		_document.Remove(2, 2);
		AreEqual("a\r\nc", _document.Text);
		CheckDocumentLines("a",
			"c");
	}

	[TestMethod]
	public void RemoveLineContentAndJoinNonMatchingDelimiters()
	{
		_document.Text = "a\nb\nc";
		_document.Remove(2, 1);
		AreEqual("a\n\nc", _document.Text);
		CheckDocumentLines("a",
			"",
			"c");
	}

	[TestMethod]
	public void RemoveLineContentAndJoinNonMatchingDelimiters2()
	{
		_document.Text = "a\nb\rc";
		_document.Remove(2, 1);
		AreEqual("a\n\rc", _document.Text);
		CheckDocumentLines("a",
			"",
			"c");
	}

	[TestMethod]
	public void RemoveMultilineUpToFirstPartOfDelimiter()
	{
		_document.Text = "0\n1\r\n2";
		_document.Remove(1, 3);
		AreEqual("0\n2", _document.Text);
		CheckDocumentLines("0",
			"2");
	}

	[TestMethod]
	public void RemoveNegativeAmount()
	{
		NUnitAssert.Throws<ArgumentOutOfRangeException>(() =>
		{
			_document.Text = "abcd";
			_document.Remove(2, -1);
		});
	}

	[TestMethod]
	public void RemoveNothing()
	{
		_document.Remove(0, 0);
		AreEqual(_document.LineCount, 1);
		AreEqual(_document.TextLength, 0);
	}

	[TestMethod]
	public void RemoveOneCharDelimiter()
	{
		_document.Text = "a\nb";
		_document.Remove(1, 1);
		AreEqual("ab", _document.Text);
		CheckDocumentLines("ab");
	}

	[TestMethod]
	public void RemoveSecondPartOfDelimiter()
	{
		_document.Text = "a\r\nb";
		_document.Remove(2, 1);
		AreEqual("a\rb", _document.Text);
		CheckDocumentLines("a",
			"b");
	}

	[TestMethod]
	public void RemoveTooMuch()
	{
		NUnitAssert.Throws<ArgumentOutOfRangeException>(() =>
		{
			_document.Text = "abcd";
			_document.Remove(2, 10);
		});
	}

	[TestMethod]
	public void RemoveTwoCharDelimiter()
	{
		_document.Text = "a\r\nb";
		_document.Remove(1, 2);
		AreEqual("ab", _document.Text);
		CheckDocumentLines("ab");
	}

	[TestMethod]
	public void RemoveUpToMatchingDelimiter1()
	{
		_document.Text = "a\r\nb\nc";
		_document.Remove(2, 2);
		AreEqual("a\r\nc", _document.Text);
		CheckDocumentLines("a",
			"c");
	}

	[TestMethod]
	public void RemoveUpToMatchingDelimiter2()
	{
		_document.Text = "a\r\nb\r\nc";
		_document.Remove(2, 3);
		AreEqual("a\r\nc", _document.Text);
		CheckDocumentLines("a",
			"c");
	}

	[TestMethod]
	public void RemoveUpToNonMatchingDelimiter()
	{
		_document.Text = "a\r\nb\rc";
		_document.Remove(2, 2);
		AreEqual("a\r\rc", _document.Text);
		CheckDocumentLines("a",
			"",
			"c");
	}

	[TestMethod]
	public void ReplaceLineContentBetweenMatchingDelimiters()
	{
		_document.Text = "a\rb\nc";
		_document.Replace(2, 1, "x");
		AreEqual("a\rx\nc", _document.Text);
		CheckDocumentLines("a",
			"x",
			"c");
	}

	[TestMethod]
	public void SetText()
	{
		_document.Text = "a";
		AreEqual(_document.LineCount, 1);
		var line = _document.GetLineByNumber(1);
		AreEqual("a", _document.GetText(line));
	}

	[TestMethod]
	public void SetTextNull()
	{
		NUnitAssert.Throws<ArgumentNullException>(() => _document.Text = null);
	}

	[TestInitialize]
	public void SetUp()
	{
		_document = new TextEditorDocument();
	}

	private void CheckDocumentLines(params string[] lines)
	{
		AreEqual(lines.Length, _document.LineCount, "LineCount");
		for (var i = 0; i < lines.Length; i++)
		{
			AreEqual(lines[i], _document.GetText(_document.Lines[i]), "Text of line " + (i + 1));
		}
	}

	#endregion
}