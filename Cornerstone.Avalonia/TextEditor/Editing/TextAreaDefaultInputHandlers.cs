#region References

using System;
using Cornerstone.Avalonia.Input;
using Cornerstone.Avalonia.TextEditor.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

/// <summary>
/// Contains the predefined input handlers.
/// </summary>
public class TextAreaDefaultInputHandler : TextAreaInputHandler
{
	#region Constructors

	/// <summary>
	/// Creates a new TextAreaDefaultInputHandler instance.
	/// </summary>
	public TextAreaDefaultInputHandler(TextArea textArea) : base(textArea)
	{
		NestedInputHandlers.Add(CaretNavigation = CaretNavigationCommandHandler.Create(textArea));
		NestedInputHandlers.Add(Editing = EditingCommandHandler.Create(textArea));
		NestedInputHandlers.Add(MouseSelection = new SelectionMouseHandler(textArea));

		AddBinding(ApplicationCommands.Undo, ExecuteUndo, CanExecuteUndo);
		AddBinding(ApplicationCommands.Redo, ExecuteRedo, CanExecuteRedo);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the caret navigation input handler.
	/// </summary>
	public TextAreaInputHandler CaretNavigation { get; }

	/// <summary>
	/// Gets the editing input handler.
	/// </summary>
	public TextAreaInputHandler Editing { get; }

	/// <summary>
	/// Gets the mouse selection input handler.
	/// </summary>
	public ITextAreaInputHandler MouseSelection { get; }

	#endregion

	#region Methods

	private void AddBinding(RoutedCommand command, EventHandler<ExecutedRoutedEventArgs> handler, EventHandler<CanExecuteRoutedEventArgs> canExecuteHandler = null)
	{
		RoutedCommandBindings.Add(new RoutedCommandBinding(command, handler, canExecuteHandler));
	}

	private void CanExecuteRedo(object sender, CanExecuteRoutedEventArgs e)
	{
		var undoStack = GetUndoStack();
		if (undoStack != null)
		{
			e.Handled = true;
			e.CanExecute = undoStack.CanRedo;
		}
	}

	private void CanExecuteUndo(object sender, CanExecuteRoutedEventArgs e)
	{
		var undoStack = GetUndoStack();
		if (undoStack != null)
		{
			e.Handled = true;
			e.CanExecute = undoStack.CanUndo;
		}
	}

	private void ExecuteRedo(object sender, ExecutedRoutedEventArgs e)
	{
		var undoStack = GetUndoStack();
		if (undoStack != null)
		{
			if (undoStack.CanRedo)
			{
				undoStack.Redo();
				TextArea.Caret.BringCaretToView();
			}
			e.Handled = true;
		}
	}

	private void ExecuteUndo(object sender, ExecutedRoutedEventArgs e)
	{
		var undoStack = GetUndoStack();
		if (undoStack != null)
		{
			if (undoStack.CanUndo)
			{
				undoStack.Undo();
				TextArea.Caret.BringCaretToView();
			}
			e.Handled = true;
		}
	}

	private UndoStack GetUndoStack()
	{
		var document = TextArea.Document;
		return document?.UndoStack;
	}

	#endregion
}