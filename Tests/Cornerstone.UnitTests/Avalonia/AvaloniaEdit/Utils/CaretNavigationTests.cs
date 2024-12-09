#region References

using Cornerstone.Text.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Utils;

[TestClass]
public class CaretNavigationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CaretStopInEmptyString()
	{
		AreEqual(0, GetNextCaretStop("", -1, CaretPositioningMode.Normal));
		AreEqual(-1, GetNextCaretStop("", 0, CaretPositioningMode.Normal));
		AreEqual(-1, GetPrevCaretStop("", 0, CaretPositioningMode.Normal));
		AreEqual(0, GetPrevCaretStop("", 1, CaretPositioningMode.Normal));

		AreEqual(-1, GetNextCaretStop("", -1, CaretPositioningMode.WordStart));
		AreEqual(-1, GetNextCaretStop("", -1, CaretPositioningMode.WordBorder));
		AreEqual(-1, GetPrevCaretStop("", 1, CaretPositioningMode.WordStart));
		AreEqual(-1, GetPrevCaretStop("", 1, CaretPositioningMode.WordBorder));
	}

	[TestMethod]
	public void CombiningMark()
	{
		var str = " x͆ ";
		AreEqual(3, GetNextCaretStop(str, 1, CaretPositioningMode.Normal));
		AreEqual(1, GetPrevCaretStop(str, 3, CaretPositioningMode.Normal));
	}

	[TestMethod]
	public void DetectWordBordersOutsideBmp()
	{
		var c = " a\U0001D49Eb ";
		AreEqual(1, GetNextCaretStop(c, 0, CaretPositioningMode.WordBorder));
		AreEqual(5, GetNextCaretStop(c, 1, CaretPositioningMode.WordBorder));

		AreEqual(5, GetPrevCaretStop(c, 6, CaretPositioningMode.WordBorder));
		AreEqual(1, GetPrevCaretStop(c, 5, CaretPositioningMode.WordBorder));
	}

	[TestMethod]
	public void DetectWordBordersOutsideBmp2()
	{
		var c = " \U0001D49E\U0001D4AA ";
		AreEqual(1, GetNextCaretStop(c, 0, CaretPositioningMode.WordBorder));
		AreEqual(5, GetNextCaretStop(c, 1, CaretPositioningMode.WordBorder));

		AreEqual(5, GetPrevCaretStop(c, 6, CaretPositioningMode.WordBorder));
		AreEqual(1, GetPrevCaretStop(c, 5, CaretPositioningMode.WordBorder));
	}

	[TestMethod]
	public void EndOfDocumentNoWordBorder()
	{
		AreEqual(4, GetNextCaretStop("txt ", 3, CaretPositioningMode.Normal));
		AreEqual(-1, GetNextCaretStop("txt ", 3, CaretPositioningMode.WordStart));
		AreEqual(-1, GetNextCaretStop("txt ", 3, CaretPositioningMode.WordBorder));

		AreEqual(4, GetPrevCaretStop("txt ", 5, CaretPositioningMode.Normal));
		AreEqual(0, GetPrevCaretStop("txt ", 5, CaretPositioningMode.WordStart));
		AreEqual(3, GetPrevCaretStop("txt ", 5, CaretPositioningMode.WordBorder));
	}

	[TestMethod]
	public void EndOfDocumentWordBorder()
	{
		AreEqual(4, GetNextCaretStop("word", 3, CaretPositioningMode.Normal));
		AreEqual(-1, GetNextCaretStop("word", 3, CaretPositioningMode.WordStart));
		AreEqual(4, GetNextCaretStop("word", 3, CaretPositioningMode.WordBorder));

		AreEqual(4, GetPrevCaretStop("word", 5, CaretPositioningMode.Normal));
		AreEqual(0, GetPrevCaretStop("word", 5, CaretPositioningMode.WordStart));
		AreEqual(4, GetPrevCaretStop("word", 5, CaretPositioningMode.WordBorder));
	}

	[TestMethod]
	public void SingleCharacterOutsideBmp()
	{
		var c = "\U0001D49E";
		AreEqual(2, GetNextCaretStop(c, 0, CaretPositioningMode.Normal));
		AreEqual(0, GetPrevCaretStop(c, 2, CaretPositioningMode.Normal));
	}

	[TestMethod]
	public void SingleClosingBraceAtLineEnd()
	{
		var str = "\t\t}";
		AreEqual(2, GetNextCaretStop(str, 1, CaretPositioningMode.WordStart));
		AreEqual(-1, GetPrevCaretStop(str, 1, CaretPositioningMode.WordStart));
	}

	[TestMethod]
	public void StackedCombiningMark()
	{
		var str = " x͆͆͆͆ ";
		AreEqual(6, GetNextCaretStop(str, 1, CaretPositioningMode.Normal));
		AreEqual(1, GetPrevCaretStop(str, 6, CaretPositioningMode.Normal));
	}

	[TestMethod]
	public void StartOfDocumentNoWordStart()
	{
		AreEqual(0, GetNextCaretStop(" word", -1, CaretPositioningMode.Normal));
		AreEqual(1, GetNextCaretStop(" word", -1, CaretPositioningMode.WordStart));
		AreEqual(1, GetNextCaretStop(" word", -1, CaretPositioningMode.WordBorder));

		AreEqual(0, GetPrevCaretStop(" word", 1, CaretPositioningMode.Normal));
		AreEqual(-1, GetPrevCaretStop(" word", 1, CaretPositioningMode.WordStart));
		AreEqual(-1, GetPrevCaretStop(" word", 1, CaretPositioningMode.WordBorder));
	}

	[TestMethod]
	public void StartOfDocumentWithWordStart()
	{
		AreEqual(0, GetNextCaretStop("word", -1, CaretPositioningMode.Normal));
		AreEqual(0, GetNextCaretStop("word", -1, CaretPositioningMode.WordStart));
		AreEqual(0, GetNextCaretStop("word", -1, CaretPositioningMode.WordBorder));

		AreEqual(0, GetPrevCaretStop("word", 1, CaretPositioningMode.Normal));
		AreEqual(0, GetPrevCaretStop("word", 1, CaretPositioningMode.WordStart));
		AreEqual(0, GetPrevCaretStop("word", 1, CaretPositioningMode.WordBorder));
	}

	private int GetNextCaretStop(string text, int offset, CaretPositioningMode mode)
	{
		return TextUtilities.GetNextCaretPosition(new StringTextSource(text), offset, LogicalDirection.Forward, mode);
	}

	private int GetPrevCaretStop(string text, int offset, CaretPositioningMode mode)
	{
		return TextUtilities.GetNextCaretPosition(new StringTextSource(text), offset, LogicalDirection.Backward, mode);
	}

	#endregion
}