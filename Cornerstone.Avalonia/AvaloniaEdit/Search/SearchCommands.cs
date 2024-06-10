#region References

using System;
using Avalonia.Input;
using Avalonia.Threading;
using Cornerstone.Avalonia.AvaloniaEdit.Editing;
using Cornerstone.Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Search;

/// <summary>
/// Search commands for AvalonEdit.
/// </summary>
public static class SearchCommands
{
	#region Fields

	/// <summary>
	/// Closes the SearchPanel.
	/// </summary>
	public static readonly RoutedCommand CloseSearchPanel = new(nameof(CloseSearchPanel), new KeyGesture(Key.Escape));

	/// <summary>
	/// Finds the next occurrence in the file.
	/// </summary>
	public static readonly RoutedCommand FindNext = new(nameof(FindNext), new KeyGesture(Key.F3));

	/// <summary>
	/// Finds the previous occurrence in the file.
	/// </summary>
	public static readonly RoutedCommand FindPrevious = new(nameof(FindPrevious), new KeyGesture(Key.F3, KeyModifiers.Shift));

	/// <summary>
	/// Replaces all the occurrences in the document.
	/// </summary>
	public static readonly RoutedCommand ReplaceAll = new(nameof(ReplaceAll), new KeyGesture(Key.A, KeyModifiers.Alt));

	/// <summary>
	/// Replaces the next occurrence in the document.
	/// </summary>
	public static readonly RoutedCommand ReplaceNext = new(nameof(ReplaceNext), new KeyGesture(Key.R, KeyModifiers.Alt));

	#endregion
}

/// <summary>
/// TextAreaInputHandler that registers all search-related commands.
/// </summary>
internal class SearchInputHandler : TextAreaInputHandler
{
	#region Fields

	private readonly SearchPanel _panel;

	#endregion

	#region Constructors

	public SearchInputHandler(TextArea textArea, SearchPanel panel)
		: base(textArea)
	{
		RegisterCommands();
		_panel = panel;
	}

	#endregion

	#region Methods

	internal void RegisterGlobalCommands()
	{
		RoutedCommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Find, ExecuteFind));
		RoutedCommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Replace, ExecuteReplace));
		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
	}

	private void CanExecuteWithOpenSearchPanel(object sender, CanExecuteRoutedEventArgs e)
	{
		if (_panel.IsClosed)
		{
			e.CanExecute = false;
			// Continue routing so that the key gesture can be consumed by another component.
			//e.ContinueRouting = true;
		}
		else
		{
			e.CanExecute = true;
			e.Handled = true;
		}
	}

	private void ExecuteCloseSearchPanel(object sender, ExecutedRoutedEventArgs e)
	{
		if (!_panel.IsClosed)
		{
			_panel.Close();
			e.Handled = true;
		}
	}

	private void ExecuteFind(object sender, ExecutedRoutedEventArgs e)
	{
		FindOrReplace(false);
	}

	private void ExecuteFindNext(object sender, ExecutedRoutedEventArgs e)
	{
		if (!_panel.IsClosed)
		{
			_panel.FindNext();
			e.Handled = true;
		}
	}

	private void ExecuteFindPrevious(object sender, ExecutedRoutedEventArgs e)
	{
		if (!_panel.IsClosed)
		{
			_panel.FindPrevious();
			e.Handled = true;
		}
	}

	private void ExecuteReplace(object sender, ExecutedRoutedEventArgs e)
	{
		FindOrReplace(true);
	}

	private void ExecuteReplaceAll(object sender, ExecutedRoutedEventArgs e)
	{
		if (!_panel.IsClosed)
		{
			_panel.ReplaceAll();
			e.Handled = true;
		}
	}

	private void ExecuteReplaceNext(object sender, ExecutedRoutedEventArgs e)
	{
		if (!_panel.IsClosed)
		{
			_panel.ReplaceNext();
			e.Handled = true;
		}
	}

	private void FindOrReplace(bool isReplaceMode)
	{
		_panel.IsReplaceMode = isReplaceMode;
		_panel.Open();
		if (!(TextArea.Selection.IsEmpty || TextArea.Selection.IsMultiline))
		{
			_panel.SearchPattern = TextArea.Selection.GetText();
		}
		Dispatcher.UIThread.Post(_panel.Reactivate, DispatcherPriority.Input);
	}

	private void RegisterCommands()
	{
		RoutedCommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Find, ExecuteFind));
		RoutedCommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Replace, ExecuteReplace));
		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.ReplaceNext, ExecuteReplaceNext, CanExecuteWithOpenSearchPanel));
		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.ReplaceAll, ExecuteReplaceAll, CanExecuteWithOpenSearchPanel));
		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.CloseSearchPanel, ExecuteCloseSearchPanel, CanExecuteWithOpenSearchPanel));
	}

	#endregion

	#region Events

	/// <summary>
	/// Fired when SearchOptions are modified inside the SearchPanel.
	/// </summary>
	public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged
	{
		add => _panel.SearchOptionsChanged += value;
		remove => _panel.SearchOptionsChanged -= value;
	}

	#endregion
}