#region References

using System.Linq;
using Cornerstone.Presentation.Controls.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class CaretManagerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddAtCreatesExtraCaretWithoutReplacingPrimary()
	{
		var model = CreateModel("abcdefghij");
		var added = model.Carets.AddAt(4);

		AreEqual(2, model.Carets.Count);
		AreEqual(model.Caret, model.Carets.Primary);
		IsNotNull(added);
		AreEqual(4, added.Offset);
		AreEqual(0, model.Caret.Offset);
		AreEqual(2, model.Carets.All.Count);
	}

	[TestMethod]
	public void AddAtOnPrimaryDoesNotRemoveLastCaret()
	{
		var model = CreateModel("abcdefghij");
		model.Caret.Move(3);

		var result = model.Carets.AddAt(3);
		AreEqual(model.Caret, result);
		AreEqual(1, model.Carets.Count);
	}

	[TestMethod]
	public void AddAtTogglesNonPrimaryAtSameOffset()
	{
		var model = CreateModel("abcdefghij");
		model.Carets.AddAt(4);
		AreEqual(2, model.Carets.Count);

		var toggled = model.Carets.AddAt(4);
		IsNull(toggled);
		AreEqual(1, model.Carets.Count);
		AreEqual(model.Caret, model.Carets.Primary);
	}

	[TestMethod]
	public void AddRelativeToPrimaryClampsColumnToShorterLine()
	{
		var model = CreateModel("abcdef\r\nab");
		model.Caret.Move(5);

		var added = model.Carets.AddRelativeToPrimary(1);
		IsNotNull(added);
		AreEqual(10, added.Offset);
	}

	[TestMethod]
	public void AddRelativeToPrimaryNoOpPastLastLine()
	{
		var model = CreateModel("only");
		IsNull(model.Carets.AddRelativeToPrimary(1));
		AreEqual(1, model.Carets.Count);
	}

	[TestMethod]
	public void AddRelativeToPrimaryPlacesCaretOnAdjacentLineSameColumn()
	{
		var model = CreateModel("hello\r\nworld\r\nmore");
		model.Caret.Move(2);

		var added = model.Carets.AddRelativeToPrimary(1);
		IsNotNull(added);
		AreEqual(2, model.Carets.Count);
		AreEqual(9, added.Offset);
		AreEqual(2, model.Caret.Offset);
	}

	[TestMethod]
	public void AddRelativeToPrimaryStacksAcrossManyLines()
	{
		var model = CreateModel("aa\r\nbb\r\ncc\r\ndd");
		model.Caret.Move(0);

		IsNotNull(model.Carets.AddRelativeToPrimary(1));
		IsNotNull(model.Carets.AddRelativeToPrimary(1));
		IsNotNull(model.Carets.AddRelativeToPrimary(1));

		AreEqual(4, model.Carets.Count);
		AreEqual(0, model.Caret.Offset);
		AreEqual(4, model.Carets.All[1].Offset);
		AreEqual(8, model.Carets.All[2].Offset);
		AreEqual(12, model.Carets.All[3].Offset);
	}

	[TestMethod]
	public void CollapseToPrimaryRemovesExtras()
	{
		var model = CreateModel("abcdefghij");
		model.Carets.AddAt(2);
		model.Carets.AddAt(6);
		IsTrue(model.Carets.CollapseToPrimary());
		AreEqual(1, model.Carets.Count);
		AreEqual(model.Caret, model.Carets.All[0]);
		IsFalse(model.Carets.CollapseToPrimary());
	}

	[TestMethod]
	public void MergeDoesNotJoinNonOverlappingCarets()
	{
		var model = CreateModel("abcdefghij");
		model.Carets.AddAt(5);
		model.Carets.MergeOverlapping();
		AreEqual(2, model.Carets.Count);
	}

	[TestMethod]
	public void MergeOverlappingEmptyCaretsAtSameOffset()
	{
		var model = CreateModel("abcdefghij");
		model.Carets.AddAt(4);
		model.Carets.All[1].Move(0);
		model.Carets.MergeOverlapping();

		AreEqual(1, model.Carets.Count);
		AreEqual(model.Caret, model.Carets.Primary);
		AreEqual(0, model.Caret.Offset);
	}

	[TestMethod]
	public void MergeOverlappingSelectionsUnionsRangeAndKeepsPrimary()
	{
		var model = CreateModel("abcdefghij");
		model.Caret.Selection.Update(0, 4);
		model.Caret.Move(4);

		var extra = model.Carets.AddAt(6);
		extra.Selection.Update(3, 8);
		extra.Move(8);

		model.Carets.MergeOverlapping();

		AreEqual(1, model.Carets.Count);
		AreEqual(model.Caret, model.Carets.Primary);
		AreEqual(0, model.Caret.Selection.StartOffset);
		AreEqual(8, model.Caret.Selection.EndOffset);
		AreEqual(8, model.Caret.Offset);
	}

	[TestMethod]
	public void RemoveCannotDropPrimary()
	{
		var model = CreateModel("abcdefghij");
		model.Carets.AddAt(5);
		model.Carets.Remove(model.Caret);
		AreEqual(2, model.Carets.Count);
		model.Carets.Remove(model.Carets.All[1]);
		AreEqual(1, model.Carets.Count);
	}

	[TestMethod]
	public void ReverseDocumentOrderHighOffsetFirst()
	{
		var model = CreateModel("abcdefghij");
		model.Caret.Move(1);
		model.Carets.AddAt(7);
		model.Carets.AddAt(3);

		var order = model.Carets.ReverseDocumentOrder();
		AreEqual(new[] { 7, 3, 1 }, order.Select(x => x.Offset).ToArray());
	}

	[TestMethod]
	public void ViewModelCaretIsPrimary()
	{
		var model = new TextEditorViewModel();
		AreEqual(1, model.Carets.Count);
		AreEqual(model.Carets.Primary, model.Caret);
	}

	private static TextEditorViewModel CreateModel(string text)
	{
		var model = new TextEditorViewModel();
		model.Load(text);
		return model;
	}

	#endregion
}