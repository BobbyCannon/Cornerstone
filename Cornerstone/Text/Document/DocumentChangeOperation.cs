#region References

using System.Diagnostics;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// Describes a change to a TextDocument.
/// </summary>
internal sealed class DocumentChangeOperation : IUndoableOperationWithContext
{
	#region Fields

	private readonly DocumentChangeEventArgs _change;
	private readonly TextEditorDocument _document;

	#endregion

	#region Constructors

	public DocumentChangeOperation(TextEditorDocument document, DocumentChangeEventArgs change)
	{
		_document = document;
		_change = change;
	}

	#endregion

	#region Methods

	public void Redo(UndoStack stack)
	{
		Debug.Assert(stack.State == UndoStack.StatePlayback);
		stack.RegisterAffectedDocument(_document);
		stack.State = UndoStack.StatePlaybackModifyDocument;
		Redo();
		stack.State = UndoStack.StatePlayback;
	}

	public void Redo()
	{
		_document.Replace(_change.Offset, _change.RemovalLength, _change.InsertedText, _change.OffsetChangeMapOrNull);
	}

	public void Undo(UndoStack stack)
	{
		Debug.Assert(stack.State == UndoStack.StatePlayback);
		stack.RegisterAffectedDocument(_document);
		stack.State = UndoStack.StatePlaybackModifyDocument;
		Undo();
		stack.State = UndoStack.StatePlayback;
	}

	public void Undo()
	{
		var map = _change.OffsetChangeMapOrNull;
		_document.Replace(_change.Offset, _change.InsertionLength, _change.RemovedText, map?.Invert());
	}

	#endregion
}