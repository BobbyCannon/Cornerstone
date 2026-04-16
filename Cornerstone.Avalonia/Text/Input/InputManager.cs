#region References

using Avalonia.Input;
using Cornerstone.Avalonia.Text.Models;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.Text.Input;

public class InputManager
{
	#region Fields

	private readonly TextEditorViewModel _viewModel;

	#endregion

	#region Constructors

	public InputManager(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;

		CommandBindings = new GapBuffer<KeyBinding>();

		InitializeBindings();
	}

	#endregion

	#region Properties

	public Buffer<KeyBinding> CommandBindings { get; }

	#endregion

	#region Methods

	public void ProcessKeyArgs(KeyEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		foreach (var commandBinding in CommandBindings)
		{
			if (!commandBinding.Gesture.Matches(args)
				|| !commandBinding.Command.CanExecute(null))
			{
				continue;
			}

			commandBinding.Command.Execute(args);

			if (args.Handled)
			{
				break;
			}
		}
	}

	private void AddBinding(KeyGesture keyGesture, KeyCommand relayCommand)
	{
		CommandBindings.Add(new KeyBinding { Gesture = keyGesture, Command = relayCommand });
	}

	private void InitializeBindings()
	{
		AddBinding(new KeyGesture(Key.Home), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.LineSmartStart, false)));
		AddBinding(new KeyGesture(Key.Home, KeyModifiers.Shift), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.LineSmartStart, true)));
		AddBinding(new KeyGesture(Key.Home, KeyModifiers.Control), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.DocumentStart, false)));
		AddBinding(new KeyGesture(Key.Home, KeyModifiers.Shift | KeyModifiers.Control), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.DocumentStart, true)));
		AddBinding(new KeyGesture(Key.End), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.LineEnd, false)));
		AddBinding(new KeyGesture(Key.End, KeyModifiers.Shift), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.LineEnd, true)));
		AddBinding(new KeyGesture(Key.End, KeyModifiers.Control), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.DocumentEnd, false)));
		AddBinding(new KeyGesture(Key.End, KeyModifiers.Shift | KeyModifiers.Control), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.DocumentEnd, true)));
		AddBinding(new KeyGesture(Key.Up), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.LineUp, false)));
		AddBinding(new KeyGesture(Key.Down), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.LineDown, false)));
		AddBinding(new KeyGesture(Key.Left), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.CharLeft, false)));
		AddBinding(new KeyGesture(Key.Right), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.CharRight, false)));
		AddBinding(new KeyGesture(Key.Up, KeyModifiers.Shift), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.LineUp, true)));
		AddBinding(new KeyGesture(Key.Down, KeyModifiers.Shift), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.LineDown, true)));
		AddBinding(new KeyGesture(Key.Left, KeyModifiers.Shift), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.CharLeft, true)));
		AddBinding(new KeyGesture(Key.Right, KeyModifiers.Shift), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.CharRight, true)));
		AddBinding(new KeyGesture(Key.PageUp), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.PageUp, false)));
		AddBinding(new KeyGesture(Key.PageUp, KeyModifiers.Shift), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.PageUp, true)));
		AddBinding(new KeyGesture(Key.PageDown), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.PageDown, false)));
		AddBinding(new KeyGesture(Key.PageDown, KeyModifiers.Shift), new KeyCommand(_ => _viewModel.Caret.Move(CaretMoveDirection.PageDown, true)));
		AddBinding(new KeyGesture(Key.A, KeyModifiers.Control), new KeyCommand(_ => _viewModel.SelectAllText()));
		AddBinding(new KeyGesture(Key.Enter), new KeyCommand(_ => _viewModel.HandleEnterKey()));
		AddBinding(new KeyGesture(Key.Return), new KeyCommand(_ => _viewModel.HandleEnterKey()));
		AddBinding(new KeyGesture(Key.Back), new KeyCommand(_ => _viewModel.Delete(_viewModel.Caret.Offset, false)));
		AddBinding(new KeyGesture(Key.Delete), new KeyCommand(_ => _viewModel.Delete(_viewModel.Caret.Offset, true)));

		AddBinding(new KeyGesture(Key.X, KeyModifiers.Control), new KeyCommand(_ => _viewModel.Clipboard.Cut(), _ => _viewModel.Clipboard.CanCut()));
		AddBinding(new KeyGesture(Key.C, KeyModifiers.Control), new KeyCommand(_ => _viewModel.Clipboard.Copy(), _ => _viewModel.Clipboard.CanCopy()));
		AddBinding(new KeyGesture(Key.V, KeyModifiers.Control), new KeyCommand(_ => _viewModel.Clipboard.Paste(), _ => _viewModel.Clipboard.CanPaste()));

		AddBinding(new KeyGesture(Key.Y, KeyModifiers.Control), new KeyCommand(_ => _viewModel.UndoManager.Redo(), _ => _viewModel.UndoManager.CanRedo()));
		AddBinding(new KeyGesture(Key.Z, KeyModifiers.Control), new KeyCommand(_ => _viewModel.UndoManager.Undo(), _ => _viewModel.UndoManager.CanUndo()));

		AddBinding(new KeyGesture(Key.Insert), new KeyCommand(_ => _viewModel.Caret.OverstrikeMode = !_viewModel.Caret.OverstrikeMode));
		AddBinding(new KeyGesture(Key.Tab), new KeyCommand(_ => _viewModel.Indent()));
		AddBinding(new KeyGesture(Key.Tab, KeyModifiers.Shift), new KeyCommand(_ => _viewModel.Unindent()));
	}

	#endregion
}