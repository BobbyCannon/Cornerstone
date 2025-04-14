#region References

using System;
using System.Diagnostics;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Document;

/// <summary>
/// This class stacks the last x operations from the undo stack and makes
/// one undo/redo operation from it.
/// </summary>
internal sealed class UndoOperationGroup : IUndoableOperationWithContext
{
	#region Fields

	private readonly IUndoableOperation[] _undolist;

	#endregion

	#region Constructors

	public UndoOperationGroup(Deque<IUndoableOperation> stack, int numops)
	{
		if (stack == null)
		{
			throw new ArgumentNullException(nameof(stack));
		}

		Debug.Assert(numops > 0, "UndoOperationGroup : numops should be > 0");
		Debug.Assert(numops <= stack.Count);

		_undolist = new IUndoableOperation[numops];
		for (var i = 0; i < numops; ++i)
		{
			_undolist[i] = stack.PopEnd();
		}
	}

	#endregion

	#region Methods

	public void Redo()
	{
		for (var i = _undolist.Length - 1; i >= 0; --i)
		{
			_undolist[i].Redo();
		}
	}

	public void Redo(UndoStack stack)
	{
		for (var i = _undolist.Length - 1; i >= 0; --i)
		{
			stack.RunRedo(_undolist[i]);
		}
	}

	public void Undo()
	{
		foreach (var t in _undolist)
		{
			t.Undo();
		}
	}

	public void Undo(UndoStack stack)
	{
		foreach (var t in _undolist)
		{
			stack.RunUndo(t);
		}
	}

	#endregion
}