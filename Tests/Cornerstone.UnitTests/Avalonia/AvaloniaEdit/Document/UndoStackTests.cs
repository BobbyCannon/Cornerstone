﻿#region References

using Cornerstone.Text.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Document;

[TestClass]
public class UndoStackTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ContinueEmptyUndoGroup()
	{
		var doc = new TextDocument();
		doc.Insert(0, "a");
		doc.UndoStack.StartUndoGroup();
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.StartContinuedUndoGroup();
		doc.Insert(1, "b");
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.Undo();
		AreEqual("a", doc.Text);
	}

	[TestMethod]
	public void ContinueEmptyUndoGroup_WithOptionalEntries()
	{
		var doc = new TextDocument();
		doc.Insert(0, "a");
		doc.UndoStack.StartUndoGroup();
		doc.UndoStack.PushOptional(new StubUndoableAction());
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.StartContinuedUndoGroup();
		doc.Insert(1, "b");
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.Undo();
		AreEqual("a", doc.Text);
	}

	[TestMethod]
	public void ContinueUndoGroup()
	{
		var doc = new TextDocument();
		doc.Insert(0, "a");
		doc.UndoStack.StartContinuedUndoGroup();
		doc.Insert(1, "b");
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.Undo();
		AreEqual("", doc.Text);
	}

	[TestMethod]
	public void EmptyContinuationGroup()
	{
		var doc = new TextDocument();
		doc.Insert(0, "a");
		doc.UndoStack.StartContinuedUndoGroup();
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.StartContinuedUndoGroup();
		doc.Insert(1, "b");
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.Undo();
		AreEqual("", doc.Text);
	}

	#endregion

	#region Classes

	private class StubUndoableAction : IUndoableOperation
	{
		#region Methods

		public void Redo()
		{
		}

		public void Undo()
		{
		}

		#endregion
	}

	#endregion
}