#region References

using System;
using System.Windows.Input;
using Avalonia.Input;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

public class ClipboardManager
{
	#region Fields

	private readonly ClipboardService _clipboard;

	private readonly TextEditorViewModel _viewModel;

	#endregion

	#region Constructors

	public ClipboardManager(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;
		_clipboard = new ClipboardService();

		CutCommand = new RelayCommand(_ => Cut(), _ => CanCut());
		CopyCommand = new RelayCommand(_ => Copy(), _ => CanCopy());
		PasteCommand = new RelayCommand(_ => Paste(), _ => CanPaste());
	}

	#endregion

	#region Properties

	public ICommand CopyCommand { get; set; }

	public ICommand CutCommand { get; set; }

	public ICommand PasteCommand { get; set; }

	#endregion

	#region Methods

	public bool CanCopy()
	{
		return _viewModel.Caret.Selection.Length > 0;
	}

	public bool CanCut()
	{
		return _viewModel.Caret.Selection.Length > 0;
	}

	public bool CanPaste()
	{
		return true;
	}

	public void Copy()
	{
		var clipboard = _clipboard;
		if ((clipboard == null) || !CanCopy())
		{
			return;
		}
		if (_viewModel.Caret.Selection.Length > 0)
		{
			var start = Math.Min(_viewModel.Caret.Selection.StartOffset, _viewModel.Caret.Selection.EndOffset);
			var selection = _viewModel.Buffer.Substring(start, _viewModel.Caret.Selection.Length);
			clipboard.SetTextAsync(selection);
		}
		else
		{
			var line = _viewModel.Caret.Line;
			var currentLine = _viewModel.Buffer.Substring(line.StartOffset, line.Length);
			clipboard.SetTextAsync(currentLine);
		}
	}

	public void Cut()
	{
		var clipboard = _clipboard;
		if ((clipboard == null) || !CanCut())
		{
			return;
		}
		if (_viewModel.Caret.Selection.Length > 0)
		{
			var start = Math.Min(_viewModel.Caret.Selection.StartOffset, _viewModel.Caret.Selection.EndOffset);
			var length = _viewModel.Caret.Selection.Length;
			var selection = _viewModel.Buffer.Substring(start, length);
			_viewModel.TryRemoveSelection(out _);
			clipboard.SetTextAsync(selection);
		}
		else
		{
			var line = _viewModel.Caret.Line;
			var currentLine = _viewModel.Buffer.Substring(line.StartOffset, line.Length);
			_viewModel.RemoveAt(line.StartOffset, line.Length);
			clipboard.SetTextAsync(currentLine);
		}
	}

	public async void Paste()
	{
		try
		{
			var clipboard = _clipboard;
			if ((clipboard == null) || !CanPaste())
			{
				return;
			}

			var text = await clipboard.GetTextAsync();
			_viewModel.ProcessTextInput(new TextInputEventArgs { Text = text });
		}
		catch
		{
			// Ignore
		}
	}

	#endregion
}