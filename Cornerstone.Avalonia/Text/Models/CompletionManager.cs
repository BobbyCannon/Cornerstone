#region References

using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Input;
using Cornerstone.Avalonia.Text.Completion;
using Cornerstone.Data;
using Cornerstone.Parsers;
using Cornerstone.Presentation;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

[SourceReflection]
public partial class CompletionManager : CornerstoneObject
{
	#region Fields

	private bool _applySingleWhenReady;
	private readonly List<CompletionItem> _allItems;
	private CompletionService _completionService;
	private int _queryGeneration;
	private readonly TextEditorViewModel _viewModel;

	#endregion

	#region Constructors

	public CompletionManager(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;
		_allItems = [];
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
		if ((start < 0) || (length < 0) || ((start + length) > _viewModel.DocumentLength))
		{
			Close();
			return false;
		}

		if (length > 0)
		{
			_viewModel.RemoveAt(start, length);
		}

		var text = item.CompletionText ?? string.Empty;
		_viewModel.Insert(start, text);
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
		Close();
		return true;
	}

	public void Close()
	{
		Interlocked.Increment(ref _queryGeneration);
		_applySingleWhenReady = false;
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
		var generation = Interlocked.Increment(ref _queryGeneration);

		if (!source.QueryOnBackgroundThread)
		{
			CompleteRequest(source, context, generation, applySingle);
			return;
		}

		IsQuerying = true;
		ThreadPool.QueueUserWorkItem(_ => RunBackgroundQuery(source, context, generation, applySingle));
	}

	/// <summary>
	/// Handle keys for an open session or a trigger. Returns true when the editor should not run the default binding.
	/// </summary>
	public bool TryHandleKey(KeyEventArgs args)
	{
		if (args == null)
		{
			return false;
		}

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

		var source = Source;
		if (source == null)
		{
			return false;
		}

		if (!source.ShouldTrigger(args.Key, args.KeyModifiers, out var silent))
		{
			return false;
		}

		if (!silent && !string.IsNullOrEmpty(args.KeySymbol))
		{
			_viewModel.ProcessTextInput(args.KeySymbol);
		}

		_applySingleWhenReady = silent;
		RequestCompletions();
		return true;
	}

	public bool TryGetCompletion(ReadOnlySpan<char> input, out ReadOnlySpan<char> completion)
	{
		if (_completionService is null)
		{
			completion = default;
			return false;
		}

		return _completionService.TryGetCompletion(input, out completion);
	}

	public void UpdateFilterFromDocument()
	{
		if (!IsOpen)
		{
			return;
		}

		var caret = _viewModel.Caret.Offset;
		if ((caret < ReplaceStart) || (caret > (ReplaceStart + ReplaceLength + 64)))
		{
			Close();
			return;
		}

		if (caret < ReplaceStart)
		{
			Close();
			return;
		}

		ReplaceLength = Math.Max(0, caret - ReplaceStart);
		if ((ReplaceStart + ReplaceLength) > _viewModel.DocumentLength)
		{
			Close();
			return;
		}

		var filter = ReplaceLength == 0
			? string.Empty
			: _viewModel.Buffer.Substring(ReplaceStart, ReplaceLength);
		ApplyFilter(filter);
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

		Open(items, replaceStart, replaceLength);
		if (applySingle && (VisibleItems.Count == 1))
		{
			ApplySelected();
		}

		IsQuerying = false;
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
					if (generation != _queryGeneration)
					{
						return;
					}

					IsQuerying = false;
					if (IsOpen)
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
				if (generation != _queryGeneration)
				{
					return;
				}

				Open(capturedItems, capturedStart, capturedLength);
				if (applySingle && (VisibleItems.Count == 1))
				{
					ApplySelected();
				}

				IsQuerying = false;
			});
		}
		catch
		{
			PostToUi(() =>
			{
				if (generation == _queryGeneration)
				{
					IsQuerying = false;
				}
			});
		}
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
				if (item.DisplayText.StartsWith(filter, StringComparison.OrdinalIgnoreCase)
					|| item.CompletionText.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
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

	private void Open(IReadOnlyList<CompletionItem> items, int replaceStart, int replaceLength)
	{
		_allItems.Clear();
		foreach (var item in items)
		{
			_allItems.Add(item);
		}

		ReplaceStart = replaceStart;
		ReplaceLength = replaceLength;
		IsOpen = true;
		var filter = (replaceLength > 0) && ((replaceStart + replaceLength) <= _viewModel.DocumentLength)
			? _viewModel.Buffer.Substring(replaceStart, replaceLength)
			: string.Empty;
		ApplyFilter(filter);
	}

	#endregion
}
