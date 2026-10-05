#region References

using System;
using System.Collections.Generic;
using System.Threading;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Controls.Text.Completion;
using Cornerstone.Data;
using Cornerstone.Text.Parsing;
using Cornerstone.Presentation;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Models;

[SourceReflection]
public partial class CompletionManager : CornerstoneObject
{
	#region Fields

	private bool _applySingleWhenReady;
	private readonly List<CompletionItem> _allItems;
	private CompletionService _completionService;
	private int _dismissWhenCaretAtOrBefore;
	private int _queryGeneration;
	private readonly TextEditorViewModel _viewModel;

	#endregion

	#region Constructors

	public CompletionManager(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;
		_allItems = [];
		_dismissWhenCaretAtOrBefore = int.MinValue;
		VisibleItems = [];
	}

	#endregion

	#region Properties

	public bool HasCompletionService => _completionService != null;

	public bool HasSource => Source != null;

	/// <summary>
	/// Optional UI dispatcher. Background queries post results here. When null, results run inline.
	/// </summary>
	public IDispatcher Dispatcher { get; set; }

	[Notify]
	public partial bool IsOpen { get; private set; }

	[Notify]
	public partial bool IsQuerying { get; private set; }

	public int ReplaceLength { get; private set; }

	public int ReplaceStart { get; private set; }

	[Notify]
	public partial CompletionItem SelectedItem { get; set; }

	public ICompletionSource Source { get; set; }

	[Notify]
	public partial IReadOnlyList<CompletionItem> VisibleItems { get; private set; }

	#endregion

	#region Methods

	public bool ApplySelected()
	{
		var item = SelectedItem;
		if (!IsOpen || (item == null))
		{
			Close();
			return false;
		}

		var start = ReplaceStart;
		var length = ReplaceLength;
		var text = item.CompletionText ?? string.Empty;
		if ((start < 0) || (length < 0) || ((start + length) > _viewModel.DocumentLength))
		{
			Close();
			return false;
		}

		// Quoted completions (paths) include wrapping quotes. If the replace span
		// stops before an existing closer, keep it in the span so we do not write "".
		length = IncludeTrailingQuoteIfDuplicated(start, length, text);
		if ((start + length) > _viewModel.DocumentLength)
		{
			Close();
			return false;
		}

		var original = _viewModel.Buffer.ToString();
		var prefixLength = Math.Min(start, original.Length);
		var suffixStart = Math.Min(start + length, original.Length);
		var expected = original.Substring(0, prefixLength) + text + original.Substring(suffixStart);
		_viewModel.UndoManager.BeginCompound();
		try
		{
			if (length > 0)
			{
				_viewModel.RemoveAt(start, length);
			}
			_viewModel.Insert(start, text);
			if (!string.Equals(_viewModel.Buffer.ToString(), expected, StringComparison.Ordinal))
			{
				_viewModel.ReplaceBufferSilent(original);
				_viewModel.UndoManager.CancelCompound();
				Close();
				return false;
			}

			var caret = start + text.Length + item.CaretDelta;
			if (caret < 0)
			{
				caret = 0;
			}
			if (caret > _viewModel.DocumentLength)
			{
				caret = _viewModel.DocumentLength;
			}

			_viewModel.Caret.Move(caret);
			_viewModel.UndoManager.EndCompound();
		}
		catch
		{
			_viewModel.ReplaceBufferSilent(original);
			_viewModel.UndoManager.CancelCompound();
			Close();
			return false;
		}

		var retrigger = item.IsDirectory;
		Close();
		if (retrigger)
		{
			RequestCompletions();
		}

		return true;
	}

	public void Close()
	{
		Interlocked.Increment(ref _queryGeneration);
		_applySingleWhenReady = false;
		_dismissWhenCaretAtOrBefore = int.MinValue;
		_allItems.Clear();
		VisibleItems = [];
		SelectedItem = null;
		ReplaceStart = 0;
		ReplaceLength = 0;
		IsQuerying = false;
		IsOpen = false;
	}

	public void Initialize(string extension)
	{
		Initialize(CompletionService.GetByExtension(extension));
	}

	public void Initialize(CompletionService completionService)
	{
		_completionService = completionService;
		NotifyComputedPropertyChanged(nameof(HasCompletionService));
	}

	public void MoveSelection(int delta)
	{
		if (!IsOpen || (VisibleItems.Count == 0))
		{
			return;
		}

		var index = 0;
		for (var i = 0; i < VisibleItems.Count; i++)
		{
			if (ReferenceEquals(VisibleItems[i], SelectedItem))
			{
				index = i;
				break;
			}
		}

		index += delta;
		if (index < 0)
		{
			index = 0;
		}
		if (index >= VisibleItems.Count)
		{
			index = VisibleItems.Count - 1;
		}

		SelectedItem = VisibleItems[index];
	}

	public void RequestCompletions()
	{
		try
		{
			var source = Source;
			if (source == null)
			{
				Close();
				return;
			}

			var context = CompletionQueryContext.FromDocument(_viewModel);
			if (!context.CanQuery)
			{
				Close();
				return;
			}

			var applySingle = _applySingleWhenReady;
			_applySingleWhenReady = false;
			var caret = _viewModel.Caret.Offset;
			_dismissWhenCaretAtOrBefore = caret > 0 ? caret - 1 : int.MinValue;
			var generation = Interlocked.Increment(ref _queryGeneration);

			if (!source.QueryOnBackgroundThread)
			{
				CompleteRequest(source, context, generation, applySingle);
				return;
			}

			IsQuerying = true;
			ThreadPool.QueueUserWorkItem(_ => RunBackgroundQuery(source, context, generation, applySingle));
		}
		catch
		{
			Close();
		}
	}

	/// <summary>
	/// Handle keys for an open session or a silent trigger (Ctrl+Space, Tab).
	/// Returns true when the editor should not run the default binding.
	/// Typed triggers (period, minus, slash) must not be consumed: TextInput inserts them
	/// and <see cref="TryTriggerFromInsertedText" /> starts the session.
	/// </summary>
	public bool TryHandleKey(KeyEventArgs args)
	{
		if (args == null)
		{
			return false;
		}

		var source = Source;

		if (IsOpen)
		{
			switch (args.Key)
			{
				case Key.Escape:
				{
					Close();
					return true;
				}
				case Key.Up:
				{
					MoveSelection(-1);
					return true;
				}
				case Key.Down:
				{
					MoveSelection(1);
					return true;
				}
				case Key.Tab:
				case Key.Enter:
				{
					ApplySelected();
					return true;
				}
			}
		}

		if (source == null)
		{
			return false;
		}

		bool silent;
		try
		{
			if (!source.ShouldTrigger(args.Key, args.KeyModifiers, out silent)
				&& !source.ShouldTriggerText(args.KeySymbol, out silent))
			{
				return false;
			}
		}
		catch
		{
			return false;
		}

		if (!silent)
		{
			return false;
		}

		_applySingleWhenReady = true;
		RequestCompletions();
		return true;
	}

	/// <summary>
	/// Start a session from an inserted character (typed ".", paste, IME).
	/// </summary>
	public bool TryTriggerFromInsertedText(string text)
	{
		var source = Source;
		bool silent;
		try
		{
			if ((source == null) || !source.ShouldTriggerText(text, out silent))
			{
				return false;
			}
		}
		catch
		{
			return false;
		}

		_applySingleWhenReady = silent;
		RequestCompletions();
		return true;
	}

	public bool TryGetCompletion(ReadOnlySpan<char> input, out ReadOnlySpan<char> completion)
	{
		completion = default;
		if (_completionService is null)
		{
			return false;
		}

		try
		{
			return _completionService.TryGetCompletion(input, out completion);
		}
		catch
		{
			completion = default;
			return false;
		}
	}

	public void UpdateFilterFromDocument()
	{
		try
		{
			UpdateFilterFromDocumentCore();
		}
		catch
		{
			Close();
		}
	}

	private void UpdateFilterFromDocumentCore()
	{
		if (!IsOpen && !IsQuerying)
		{
			return;
		}

		var caret = _viewModel.Caret.Offset;
		if (ShouldDismissForCaret(caret))
		{
			Close();
			return;
		}

		if (!IsOpen)
		{
			return;
		}

		if (caret > (ReplaceStart + ReplaceLength))
		{
			ReplaceLength = caret - ReplaceStart;
		}

		if ((ReplaceStart + ReplaceLength) > _viewModel.DocumentLength)
		{
			ReplaceLength = Math.Max(0, _viewModel.DocumentLength - ReplaceStart);
		}

		ApplyFilter(GetFilterText());
		if (VisibleItems.Count == 0)
		{
			Close();
		}
	}

	private void CompleteRequest(
		ICompletionSource source,
		CompletionQueryContext context,
		int generation,
		bool applySingle)
	{
		try
		{
			if (generation != _queryGeneration)
			{
				return;
			}

			if (!source.TryGetCompletions(context, out var items, out var replaceStart, out var replaceLength)
				|| (items == null)
				|| (items.Count == 0))
			{
				if (generation == _queryGeneration)
				{
					IsQuerying = false;
					if (IsOpen)
					{
						Close();
					}
				}

				return;
			}

			if (generation != _queryGeneration)
			{
				return;
			}

			Open(items, replaceStart, replaceLength, context.CaretOffset);
			if (applySingle
				&& (VisibleItems.Count == 1)
				&& (_viewModel.Caret.Offset == context.CaretOffset))
			{
				ApplySelected();
			}

			IsQuerying = false;
		}
		catch
		{
			if (generation == _queryGeneration)
			{
				IsQuerying = false;
				Close();
			}
		}
	}

	private void PostToUi(Action action)
	{
		if (action == null)
		{
			return;
		}

		var dispatcher = Dispatcher;
		if (dispatcher != null)
		{
			dispatcher.Dispatch(action);
			return;
		}

		action();
	}

	private void RunBackgroundQuery(
		ICompletionSource source,
		CompletionQueryContext context,
		int generation,
		bool applySingle)
	{
		try
		{
			if (generation != Volatile.Read(ref _queryGeneration))
			{
				return;
			}

			if (!source.TryGetCompletions(context, out var items, out var replaceStart, out var replaceLength)
				|| (items == null)
				|| (items.Count == 0))
			{
				PostToUi(() =>
				{
					try
					{
						if (generation != _queryGeneration)
						{
							return;
						}

						IsQuerying = false;
						if (IsOpen)
						{
							Close();
						}
					}
					catch
					{
						Close();
					}
				});
				return;
			}

			// Capture for the UI thread; do not read the live document here.
			var capturedItems = items;
			var capturedStart = replaceStart;
			var capturedLength = replaceLength;
			PostToUi(() =>
			{
				try
				{
					if (generation != _queryGeneration)
					{
						return;
					}

					Open(capturedItems, capturedStart, capturedLength, context.CaretOffset);
					if (applySingle
						&& (VisibleItems.Count == 1)
						&& (_viewModel.Caret.Offset == context.CaretOffset))
					{
						ApplySelected();
					}

					IsQuerying = false;
				}
				catch
				{
					Close();
				}
			});
		}
		catch
		{
			PostToUi(() =>
			{
				try
				{
					if (generation == _queryGeneration)
					{
						IsQuerying = false;
					}
				}
				catch
				{
					Close();
				}
			});
		}
	}

	private int IncludeTrailingQuoteIfDuplicated(int start, int length, string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return length;
		}

		var closer = text[text.Length - 1];
		if (closer is not ('"' or '\''))
		{
			return length;
		}

		var end = start + length;
		if ((end < _viewModel.DocumentLength) && (_viewModel.Buffer[end] == closer))
		{
			return length + 1;
		}

		return length;
	}

	private void ApplyFilter(string filter)
	{
		if (string.IsNullOrEmpty(filter))
		{
			VisibleItems = _allItems.ToArray();
		}
		else
		{
			var matches = new List<CompletionItem>();
			foreach (var item in _allItems)
			{
				if (MatchesFilter(item.DisplayText, filter)
					|| MatchesFilter(item.CompletionText, filter))
				{
					matches.Add(item);
				}
			}

			VisibleItems = matches;
		}

		if (VisibleItems.Count == 0)
		{
			SelectedItem = null;
			return;
		}

		if ((SelectedItem == null) || !ContainsItem(VisibleItems, SelectedItem))
		{
			SelectedItem = VisibleItems[0];
		}
	}

	private static bool MatchesFilter(string value, string filter)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}

		if (value.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		var unquotedValue = UnwrapMatchingQuotes(value);
		var unquotedFilter = UnwrapMatchingQuotes(filter);
		if ((unquotedValue != value) || (unquotedFilter != filter))
		{
			return unquotedValue.StartsWith(unquotedFilter, StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}

	private static string UnwrapMatchingQuotes(string value)
	{
		if ((value == null) || (value.Length < 2))
		{
			return value ?? string.Empty;
		}

		var quote = value[0];
		if ((quote is not ('"' or '\'')) || (value[value.Length - 1] != quote))
		{
			return value;
		}

		return value.Substring(1, value.Length - 2);
	}

	private static bool ContainsItem(IReadOnlyList<CompletionItem> items, CompletionItem item)
	{
		for (var i = 0; i < items.Count; i++)
		{
			if (ReferenceEquals(items[i], item))
			{
				return true;
			}
		}

		return false;
	}

	private void Open(IReadOnlyList<CompletionItem> items, int replaceStart, int replaceLength, int queryCaretOffset)
	{
		_allItems.Clear();
		foreach (var item in items)
		{
			_allItems.Add(item);
		}

		ReplaceStart = replaceStart;
		ReplaceLength = replaceLength;
		var caret = _viewModel.Caret.Offset;
		// User typed past the snapshot (slow first query). Grow the replace span so
		// apply/filter include those characters. Never rewrite the document here.
		if ((caret > queryCaretOffset) && (caret > (ReplaceStart + ReplaceLength)))
		{
			ReplaceLength = caret - ReplaceStart;
		}

		IsOpen = true;
		if (ShouldDismissForCaret(caret))
		{
			Close();
			return;
		}

		ApplyFilter(GetFilterText());
		if (VisibleItems.Count == 0)
		{
			Close();
		}
	}

	private bool ShouldDismissForCaret(int caret)
	{
		if (caret <= _dismissWhenCaretAtOrBefore)
		{
			return true;
		}

		return (caret < ReplaceStart) || (caret > (ReplaceStart + ReplaceLength + 64));
	}

	/// <summary>
	/// Prefix used to filter items. Characters after the caret (for example a closing quote)
	/// stay in the replace span for apply but must not be part of the filter.
	/// </summary>
	private string GetFilterText()
	{
		var caret = _viewModel.Caret.Offset;
		if (caret < ReplaceStart)
		{
			return string.Empty;
		}

		var filterLength = caret - ReplaceStart;
		if ((filterLength <= 0) || ((ReplaceStart + filterLength) > _viewModel.DocumentLength))
		{
			return string.Empty;
		}

		return _viewModel.Buffer.Substring(ReplaceStart, filterLength);
	}

	#endregion
}
