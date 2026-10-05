#region References

using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Controls.Text.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class MultiCaretEditTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void DeleteBackwardsAtEveryCaret()
	{
		var model = Create("abcde");
		model.Caret.Move(1);
		model.Carets.AddAt(4);
		model.Delete(0, false);

		AreEqual("bce", model.ToString());
		AreEqual(2, model.Carets.Count);
		AreEqual(0, model.Caret.Offset);
		AreEqual(2, model.Carets.All[1].Offset);
	}

	[TestMethod]
	public void IndentAtEveryCaret()
	{
		var model = Create("ab\r\ncd");
		model.Caret.Move(0);
		model.Carets.AddAt(4);
		model.Indent();

		AreEqual("\tab\r\n\tcd", model.ToString());
	}

	[TestMethod]
	public void ProcessTextInputInsertsAtEveryCaret()
	{
		var model = Create("abcde");
		model.Caret.Move(1);
		model.Carets.AddAt(4);
		model.ProcessTextInput("X");

		AreEqual("aXbcdXe", model.ToString());
		AreEqual(2, model.Carets.Count);
		AreEqual(2, model.Caret.Offset);
		AreEqual(6, model.Carets.All[1].Offset);
	}

	[TestMethod]
	public void ProcessTextInputReplacesEverySelection()
	{
		var model = Create("abcdef");
		model.Caret.Selection.Update(0, 2);
		model.Caret.Move(2);
		var extra = model.Carets.AddAt(4);
		extra.Selection.Update(4, 6);
		extra.Move(6);

		model.ProcessTextInput("Z");

		AreEqual("ZcdZ", model.ToString());
		AreEqual(2, model.Carets.Count);
	}

	[TestMethod]
	public void ReadOnlySectionSkipsOneCaret()
	{
		var model = Create("abcdef");
		model.ReadOnlySectionProvider = new PrefixReadOnlyProvider(3);
		model.Caret.Move(1);
		model.Carets.AddAt(5);
		model.ProcessTextInput("X");

		AreEqual("abcdeXf", model.ToString());
		AreEqual(1, model.Caret.Offset);
		AreEqual(6, model.Carets.All[1].Offset);
	}

	[TestMethod]
	public void SingleCaretProcessTextInputStillOneSite()
	{
		var model = Create("ab");
		model.Caret.Move(1);
		model.ProcessTextInput("X");
		AreEqual("aXb", model.ToString());
		AreEqual(1, model.Carets.Count);
		AreEqual(2, model.Caret.Offset);
	}

	[TestMethod]
	public void UndoRestoresBufferAndCaretSet()
	{
		var model = Create("abcde");
		model.Caret.Move(1);
		model.Carets.AddAt(4);
		model.ProcessTextInput("X");
		AreEqual("aXbcdXe", model.ToString());
		AreEqual(2, model.Carets.Count);

		model.UndoManager.Undo();
		AreEqual("abcde", model.ToString());
		AreEqual(2, model.Carets.Count);
		AreEqual(1, model.Caret.Offset);
		AreEqual(4, model.Carets.All[1].Offset);

		model.UndoManager.Redo();
		AreEqual("aXbcdXe", model.ToString());
		AreEqual(2, model.Carets.Count);
		AreEqual(2, model.Caret.Offset);
		AreEqual(6, model.Carets.All[1].Offset);
	}

	private static TextEditorViewModel Create(string text)
	{
		var model = new TextEditorViewModel();
		model.Load(text);
		return model;
	}

	#endregion

	#region Classes

	private sealed class PrefixReadOnlyProvider : IReadOnlySectionProvider
	{
		#region Fields

		private readonly int _end;

		#endregion

		#region Constructors

		public PrefixReadOnlyProvider(int end)
		{
			_end = end;
		}

		#endregion

		#region Methods

		public bool CanModify(int offset)
		{
			return offset >= _end;
		}

		public IEnumerable<IRange> GetDeletableSegments(IRange range)
		{
			var start = range.StartOffset < _end ? _end : range.StartOffset;
			if (start >= range.EndOffset)
			{
				yield break;
			}

			yield return new Range
			{
				StartOffset = start,
				EndOffset = range.EndOffset
			};
		}

		#endregion
	}

	#endregion
}