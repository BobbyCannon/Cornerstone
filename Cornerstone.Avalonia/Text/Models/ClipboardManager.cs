#region References

using System;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Range = Cornerstone.Collections.Range;

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
		if (_viewModel.IsReadOnly)
		{
			return false;
		}

		return GetDeletableRangeText(GetCutRequestRange()) != null;
	}

	public bool CanPaste()
	{
		if (_viewModel.IsReadOnly)
		{
			return false;
		}

		return _viewModel.ReadOnlySectionProvider?.CanModify(_viewModel.Caret.Offset) ?? true;
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
		if (clipboard == null)
		{
			return;
		}

		var request = GetCutRequestRange();
		var text = GetDeletableRangeText(request);
		if ((text == null) || (request == null))
		{
			return;
		}

		// Ensure selection matches the range we evaluated (line-cut selects the line first).
		if (_viewModel.Caret.Selection.Length <= 0)
		{
			_viewModel.Caret.Selection.Update(request.StartOffset, request.EndOffset);
		}

		if (!_viewModel.TryRemoveSelection(out _))
		{
			return;
		}

		clipboard.SetTextAsync(text);
	}

	/// <summary>
	/// Selection range if present; otherwise the current line (for whole-line cut).
	/// </summary>
	private Range GetCutRequestRange()
	{
		if (_viewModel.Caret.Selection.Length > 0)
		{
			var start = Math.Min(_viewModel.Caret.Selection.StartOffset, _viewModel.Caret.Selection.EndOffset);
			var end = Math.Max(_viewModel.Caret.Selection.StartOffset, _viewModel.Caret.Selection.EndOffset);
			return new Range
			{
				StartOffset = start,
				EndOffset = end
			};
		}

		var line = _viewModel.Caret.Line;
		if ((line == null) || (line.Length <= 0))
		{
			return null;
		}

		return new Range
		{
			StartOffset = line.StartOffset,
			EndOffset = line.StartOffset + line.Length
		};
	}

	/// <summary>
	/// Text that would be removed for <paramref name="request" />, or null if nothing is deletable.
	/// </summary>
	private string GetDeletableRangeText(Range request)
	{
		if ((request == null) || (request.Length <= 0))
		{
			return null;
		}

		if (_viewModel.ReadOnlySectionProvider == null)
		{
			return _viewModel.Buffer.Substring(request.StartOffset, request.Length);
		}

		var segments = _viewModel.ReadOnlySectionProvider
			.GetDeletableSegments(request)
			.Where(static s => s.Length > 0)
			.OrderBy(static s => s.StartOffset)
			.ToList();

		if (segments.Count == 0)
		{
			return null;
		}

		if (segments.Count == 1)
		{
			var only = segments[0];
			return _viewModel.Buffer.Substring(only.StartOffset, only.Length);
		}

		var builder = new StringBuilder();
		foreach (var segment in segments)
		{
			builder.Append(_viewModel.Buffer.Substring(segment.StartOffset, segment.Length));
		}

		return builder.ToString();
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
			_viewModel.ProcessTextInput(text);
		}
		catch
		{
			// Ignore
		}
	}

	#endregion
}