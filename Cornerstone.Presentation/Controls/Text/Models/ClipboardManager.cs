#region References

using System;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Input.Platform;
using Range = Cornerstone.Collections.Range;
using Cornerstone.Presentation.Controls.Chrome;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Models;

public class ClipboardManager
{
	#region Fields

	private readonly TextEditorViewModel _viewModel;

	#endregion

	#region Constructors

	public ClipboardManager(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;

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
		if (_viewModel.Carets.Count > 1)
		{
			return GetCopyText() != null;
		}

		return _viewModel.Caret.Selection.Length > 0;
	}

	public bool CanCut()
	{
		if (_viewModel.IsReadOnly)
		{
			return false;
		}

		if (_viewModel.Carets.Count > 1)
		{
			foreach (var caret in _viewModel.Carets.All)
			{
				if (GetDeletableRangeText(GetCopyPayloadRange(caret)) != null)
				{
					return true;
				}
			}

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

		var provider = _viewModel.ReadOnlySectionProvider;
		if (provider == null)
		{
			return true;
		}

		foreach (var caret in _viewModel.Carets.All)
		{
			if (provider.CanModify(caret.Offset))
			{
				return true;
			}
		}

		return false;
	}

	public void Copy()
	{
		var clipboard = GetClipboard();
		var text = GetCopyText();
		if ((clipboard == null) || (text == null))
		{
			return;
		}

		clipboard.SetTextAsync(text);
	}

	public void Cut()
	{
		var clipboard = GetClipboard();

		if (_viewModel.Carets.Count > 1)
		{
			var text = GetCopyText();
			if (text == null)
			{
				return;
			}

			_viewModel.DeleteCopyPayloads();
			clipboard?.SetTextAsync(text);
			return;
		}

		var request = GetCutRequestRange();
		var cutText = GetDeletableRangeText(request);
		if ((cutText == null) || (request == null))
		{
			return;
		}

		if (_viewModel.Caret.Selection.Length <= 0)
		{
			_viewModel.Caret.Selection.Update(request.StartOffset, request.EndOffset);
		}

		if (!_viewModel.TryRemoveSelection(out _))
		{
			return;
		}

		clipboard?.SetTextAsync(cutText);
	}

	/// <summary>
	/// Clipboard payload: selections joined with \r\n in document order, or the
	/// whole line at a caret with no selection.
	/// </summary>
	public string GetCopyText()
	{
		var builder = new StringBuilder();
		var any = false;
		foreach (var caret in _viewModel.Carets.DocumentOrder())
		{
			var range = GetCopyPayloadRange(caret);
			var piece = GetRangeText(range);
			if (piece == null)
			{
				continue;
			}

			if (any)
			{
				builder.Append("\r\n");
			}

			builder.Append(piece);
			any = true;
		}

		return any ? builder.ToString() : null;
	}

	public async void Paste()
	{
		try
		{
			var clipboard = GetClipboard();
			if ((clipboard == null) || !CanPaste())
			{
				return;
			}

			var text = await clipboard.TryGetTextAsync();
			_viewModel.ProcessPaste(text);
		}
		catch
		{
			// Ignore
		}
	}

	private Range GetCopyPayloadRange(Caret caret)
	{
		if (caret.Selection.Length > 0)
		{
			var start = Math.Min(caret.Selection.StartOffset, caret.Selection.EndOffset);
			var end = Math.Max(caret.Selection.StartOffset, caret.Selection.EndOffset);
			return new Range
			{
				StartOffset = start,
				EndOffset = end
			};
		}

		var line = caret.Line ?? _viewModel.Lines.GetLineFromOffset(caret.Offset);
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
	/// Selection range if present; otherwise the current line (for whole-line cut).
	/// </summary>
	private Range GetCutRequestRange()
	{
		return GetCopyPayloadRange(_viewModel.Caret);
	}

	/// <summary>
	/// Text that would be removed for request, or null if nothing is deletable.
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

	private string GetRangeText(Range request)
	{
		if ((request == null) || (request.Length <= 0))
		{
			return null;
		}

		return _viewModel.Buffer.Substring(request.StartOffset, request.Length);
	}

	private static IClipboard GetClipboard()
	{
		return Application.Current?.ApplicationLifetime switch
		{
			IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow?.Clipboard,
			ISingleViewApplicationLifetime single => TopLevel.GetTopLevel(single.MainView)?.Clipboard,
			_ => null
		};
	}

	#endregion
}
