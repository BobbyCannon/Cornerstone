namespace Cornerstone.Text.Document;

/// <summary>
/// This Interface describes a the basic Undo/Redo operation
/// all Undo Operations must implement this interface.
/// </summary>
public interface IUndoableOperation
{
	#region Methods

	/// <summary>
	/// Redo the last operation
	/// </summary>
	void Redo();

	/// <summary>
	/// Undo the last operation
	/// </summary>
	void Undo();

	#endregion
}

internal interface IUndoableOperationWithContext : IUndoableOperation
{
	#region Methods

	void Redo(UndoStack stack);
	void Undo(UndoStack stack);

	#endregion
}