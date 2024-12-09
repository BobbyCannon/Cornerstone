#region References

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Cornerstone.Avalonia.AvaloniaEdit.Editing;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;
using Cornerstone.Avalonia.Input;
using Cornerstone.Collections;
using Cornerstone.Text.Document;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Search;

/// <summary>
/// Provides search functionality for AvalonEdit. It is displayed in the top-right corner of the TextArea.
/// </summary>
[DoNotNotify]
public class SearchPanel : TemplatedControl, IRoutedCommandHandler
{
	#region Fields

	public static readonly StyledProperty<bool> IsReplaceModeProperty;

	/// <summary>
	/// Dependency property for <see cref="MatchCase" />.
	/// </summary>
	public static readonly StyledProperty<bool> MatchCaseProperty;

	public static readonly StyledProperty<string> ReplacePatternProperty;

	/// <summary>
	/// Dependency property for <see cref="SearchPattern" />.
	/// </summary>
	public static readonly StyledProperty<string> SearchPatternProperty;

	/// <summary>
	/// Dependency property for <see cref="UseRegex" />.
	/// </summary>
	public static readonly StyledProperty<bool> UseRegexProperty;

	/// <summary>
	/// Dependency property for <see cref="WholeWords" />.
	/// </summary>
	public static readonly StyledProperty<bool> WholeWordsProperty;

	private Border _border;
	private TextEditorDocument _currentDocument;
	private int _currentSearchResultIndex;
	private SearchInputHandler _handler;

	private Panel _messageView;
	private TextBlock _messageViewContent;
	private SearchResultBackgroundRenderer _renderer;
	private TextBox _replaceTextBox;
	private TextBox _searchTextBox;

	private ISearchStrategy _strategy;
	private TextArea _textArea;

	#endregion

	#region Constructors

	public SearchPanel()
	{
		_currentSearchResultIndex = -1;

		RoutedCommandBindings = [];
	}

	static SearchPanel()
	{
		IsReplaceModeProperty = AvaloniaProperty.Register<SearchPanel, bool>(nameof(IsReplaceMode));
		MatchCaseProperty = AvaloniaProperty.Register<SearchPanel, bool>(nameof(MatchCase));
		ReplacePatternProperty = AvaloniaProperty.Register<SearchPanel, string>(nameof(ReplacePattern));
		SearchPatternProperty = AvaloniaProperty.Register<SearchPanel, string>(nameof(SearchPattern), "");
		UseRegexProperty = AvaloniaProperty.Register<SearchPanel, bool>(nameof(UseRegex));
		WholeWordsProperty = AvaloniaProperty.Register<SearchPanel, bool>(nameof(WholeWords));

		UseRegexProperty.Changed.Subscribe(SearchPatternChangedCallback);
		MatchCaseProperty.Changed.Subscribe(SearchPatternChangedCallback);
		WholeWordsProperty.Changed.Subscribe(SearchPatternChangedCallback);
		SearchPatternProperty.Changed.Subscribe(SearchPatternChangedCallback);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets whether the Panel is already closed.
	/// </summary>
	public bool IsClosed { get; private set; }

	/// <summary>
	/// Gets whether the Panel is currently opened.
	/// </summary>
	public bool IsOpened => !IsClosed;

	public bool IsReplaceMode
	{
		get => GetValue(IsReplaceModeProperty);
		set => SetValue(IsReplaceModeProperty, !(TextEditor?.IsReadOnly ?? false) && value);
	}

	/// <summary>
	/// Gets/sets whether the search pattern should be interpreted case-sensitive.
	/// </summary>
	public bool MatchCase
	{
		get => GetValue(MatchCaseProperty);
		set => SetValue(MatchCaseProperty, value);
	}

	public string ReplacePattern
	{
		get => GetValue(ReplacePatternProperty);
		set => SetValue(ReplacePatternProperty, value);
	}

	public SpeedyList<RoutedCommandBinding> RoutedCommandBindings { get; }

	/// <summary>
	/// Gets/sets the search pattern.
	/// </summary>
	public string SearchPattern
	{
		get => GetValue(SearchPatternProperty);
		set => SetValue(SearchPatternProperty, value);
	}

	public TextEditorControl TextEditor { get; private set; }

	/// <summary>
	/// Gets/sets whether the search pattern should be interpreted as regular expression.
	/// </summary>
	public bool UseRegex
	{
		get => GetValue(UseRegexProperty);
		set => SetValue(UseRegexProperty, value);
	}

	/// <summary>
	/// Gets/sets whether the search pattern should only match whole words.
	/// </summary>
	public bool WholeWords
	{
		get => GetValue(WholeWordsProperty);
		set => SetValue(WholeWordsProperty, value);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Closes the SearchPanel.
	/// </summary>
	public void Close()
	{
		_textArea.RemoveChild(this);

		if (_messageView != null)
		{
			_messageView.IsVisible = false;
		}

		_textArea.TextView.BackgroundRenderers.Remove(_renderer);

		IsClosed = true;

		// Clear existing search results so that the segments don't have to be maintained
		_renderer.CurrentResults.Clear();
		_currentSearchResultIndex = -1;

		_textArea.Focus();
	}

	/// <summary>
	/// Moves to the next occurrence in the file starting at the next position from current caret offset.
	/// </summary>
	public void FindNext(int startOffset = -1)
	{
		var result = _renderer.CurrentResults.FindFirstSegmentWithStartAfter(startOffset == -1 ? _textArea.Caret.Offset : startOffset) ??
			_renderer.CurrentResults.FirstSegment;
		if (result != null)
		{
			SetCurrentSearchResult(result);
		}
	}

	/// <summary>
	/// Moves to the previous occurrence in the file.
	/// </summary>
	public void FindPrevious()
	{
		var result = _renderer.CurrentResults.FindFirstSegmentWithStartAfter(
			Math.Max(_textArea.Caret.Offset - _textArea.Selection.Length, 0)
		);
		if (result != null)
		{
			result = _renderer.CurrentResults.GetPreviousSegment(result);
		}

		result ??= _renderer.CurrentResults.LastSegment;

		if (result != null)
		{
			SetCurrentSearchResult(result);
		}
	}

	/// <summary>
	/// Creates a SearchPanel and installs it to the TextEditor's TextArea.
	/// </summary>
	/// <remarks> This is a convenience wrapper. </remarks>
	public static SearchPanel Install(TextEditorControl editor)
	{
		if (editor == null)
		{
			throw new ArgumentNullException(nameof(editor));
		}
		if (editor.TextArea == null)
		{
			throw new ArgumentNullException(nameof(editor.TextArea));
		}

		var textArea = editor.TextArea;

		var panel = new SearchPanel();
		panel.AttachInternal(editor);
		panel._handler = new SearchInputHandler(textArea, panel);
		textArea.DefaultInputHandler.NestedInputHandlers.Add(panel._handler);
		((ISetLogicalParent) panel).SetParent(textArea);
		return panel;
	}

	/// <summary>
	/// Opens the existing search panel.
	/// </summary>
	public void Open()
	{
		if (!IsClosed)
		{
			return;
		}

		_textArea.AddChild(this);
		_textArea.TextView.BackgroundRenderers.Add(_renderer);

		IsClosed = false;
		DoSearch(false);
	}

	/// <summary>
	/// Reactivates the SearchPanel by setting the focus on the search box and selecting all text.
	/// </summary>
	public void Reactivate()
	{
		if (_searchTextBox == null)
		{
			return;
		}

		_searchTextBox.Focus();
		_searchTextBox.SelectionStart = 0;
		_searchTextBox.SelectionEnd = _searchTextBox.Text?.Length ?? 0;
	}

	public void ReplaceAll()
	{
		if (!IsReplaceMode)
		{
			return;
		}

		var replacement = ReplacePattern ?? string.Empty;
		var document = _textArea.Document;
		using (document.RunUpdate())
		{
			var segments = _renderer.CurrentResults.OrderByDescending(x => x.EndIndex).ToArray();
			foreach (var textSegment in segments)
			{
				document.Replace(textSegment.StartOffset, textSegment.Length,
					new StringTextSource(replacement));
			}
		}
	}

	public void ReplaceNext()
	{
		if (!IsReplaceMode)
		{
			return;
		}

		FindNext(Math.Max(_textArea.Caret.Offset - _textArea.Selection.Length, 0));

		if (!_textArea.Selection.IsEmpty)
		{
			_textArea.Selection.ReplaceSelectionWithText(ReplacePattern ?? string.Empty);
		}

		UpdateSearch();
	}

	public void SetSearchResultsBrush(IBrush brush)
	{
		if (_renderer == null)
		{
			return;
		}

		_renderer.MarkerBrush = brush;
		TextEditor.TextArea.TextView.InvalidateVisual();
	}

	/// <summary>
	/// Removes the SearchPanel from the TextArea.
	/// </summary>
	public void Uninstall()
	{
		Close();
		_textArea.DocumentChanged -= TextArea_DocumentChanged;
		if (_currentDocument != null)
		{
			_currentDocument.TextChanged -= TextArea_Document_TextChanged;
		}
		_textArea.DefaultInputHandler.NestedInputHandlers.Remove(_handler);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_border = e.NameScope.Find<Border>("PART_Border");
		_searchTextBox = e.NameScope.Find<TextBox>("PART_searchTextBox");
		_replaceTextBox = e.NameScope.Find<TextBox>("ReplaceBox");
		_messageView = e.NameScope.Find<Panel>("PART_MessageView");
		_messageViewContent = e.NameScope.Find<TextBlock>("PART_MessageContent");
	}

	protected override void OnGotFocus(GotFocusEventArgs e)
	{
		e.Handled = true;

		base.OnGotFocus(e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		Cursor = Cursor.Default;
		base.OnPointerMoved(e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		e.Handled = true;

		base.OnPointerPressed(e);
	}

	/// <summary>
	/// Raises the <see cref="SearchOptionsChanged" /> event.
	/// </summary>
	protected virtual void OnSearchOptionsChanged(SearchOptionsChangedEventArgs e)
	{
		SearchOptionsChanged?.Invoke(this, e);
	}

	private void AttachInternal(TextEditorControl textEditor)
	{
		TextEditor = textEditor;
		_textArea = textEditor.TextArea;

		_renderer = new SearchResultBackgroundRenderer(textEditor.SearchResultsBrush);
		_currentDocument = _textArea.Document;
		if (_currentDocument != null)
		{
			_currentDocument.TextChanged += TextArea_Document_TextChanged;
		}
		_textArea.DocumentChanged += TextArea_DocumentChanged;
		KeyDown += SearchLayerKeyDown;

		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.FindNext, (sender, e) => FindNext()));
		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.FindPrevious, (sender, e) => FindPrevious()));
		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.CloseSearchPanel, (sender, e) => Close()));
		RoutedCommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Find, (sender, e) =>
		{
			IsReplaceMode = false;
			Reactivate();
		}));
		RoutedCommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Replace, (sender, e) => IsReplaceMode = true));
		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.ReplaceNext, (sender, e) => ReplaceNext(), (sender, e) => e.CanExecute = IsReplaceMode));
		RoutedCommandBindings.Add(new RoutedCommandBinding(SearchCommands.ReplaceAll, (sender, e) => ReplaceAll(), (sender, e) => e.CanExecute = IsReplaceMode));

		IsClosed = true;
	}

	private void CleanSearchResults()
	{
		_renderer.CurrentResults.Clear();
		_currentSearchResultIndex = -1;
	}

	private void DoSearch(bool changeSelection)
	{
		if (IsClosed)
		{
			return;
		}

		CleanSearchResults();

		var offset = Math.Max(_textArea.Caret.Offset - _textArea.Selection.Length, 0);

		if (changeSelection)
		{
			_textArea.ClearSelection();
		}

		if (!string.IsNullOrEmpty(SearchPattern))
		{
			// We cast from ISearchResult to SearchResult; this is safe because we always use the built-in strategy
			foreach (var result in _strategy.FindAll(_textArea.Document, 0, _textArea.Document.TextLength).Cast<SearchResult>())
			{
				_renderer.CurrentResults.Add(result);
			}

			if (changeSelection)
			{
				// select the first result after the caret position
				// or the first result in document order if there is no result after the caret
				var result = _renderer.CurrentResults.FindFirstSegmentWithStartAfter(offset) ??
					_renderer.CurrentResults.FirstSegment;

				if (result != null)
				{
					SelectResult(result);
				}

				_currentSearchResultIndex = _renderer.CurrentResults.Count - 1;
			}
		}

		UpdateSearchLabel();
		_textArea.TextView.InvalidateLayer(KnownLayer.Selection);
	}

	private static int GetSearchResultIndex(TextSegmentCollection<SearchResult> searchResults, SearchResult match)
	{
		var index = 0;
		foreach (var searchResult in searchResults)
		{
			if (searchResult.Equals(match))
			{
				return index;
			}

			index++;
		}

		return -1;
	}

	private void SearchLayerKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
			case Key.Enter:
			{
				e.Handled = true;
				if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
				{
					FindPrevious();
				}
				else
				{
					FindNext();
				}
				break;
			}
			case Key.Escape:
			{
				e.Handled = true;
				Close();
				break;
			}
			case Key.Tab:
			{
				e.Handled = true;
				if (_searchTextBox.IsFocused && IsReplaceMode)
				{
					_replaceTextBox.Focus();
				}
				else if (!_searchTextBox.IsFocused)
				{
					_searchTextBox.Focus();
				}
				break;
			}
		}
	}

	private static void SearchPatternChangedCallback(AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Sender is SearchPanel panel)
		{
			panel.UpdateSearch();
		}
	}

	private void SelectResult(TextRange result)
	{
		_textArea.Caret.Offset = result.EndIndex;
		_textArea.Selection = Selection.Create(_textArea, result.StartOffset, result.EndIndex);

		var distanceToViewBorder = _border == null ? Caret.MinimumDistanceToViewBorder : _border.Bounds.Height + _textArea.TextView.DefaultLineHeight;
		_textArea.Caret.BringCaretToView(distanceToViewBorder);

		// show caret even if the editor does not have the Keyboard Focus
		_textArea.Caret.Show();
	}

	private void SetCurrentSearchResult(SearchResult result)
	{
		_currentSearchResultIndex = GetSearchResultIndex(_renderer.CurrentResults, result);
		SelectResult(result);
		UpdateSearchLabel();
	}

	private void TextArea_Document_TextChanged(object sender, EventArgs e)
	{
		DoSearch(false);
	}

	private void TextArea_DocumentChanged(object sender, EventArgs e)
	{
		if (_currentDocument != null)
		{
			_currentDocument.TextChanged -= TextArea_Document_TextChanged;
		}
		_currentDocument = _textArea.Document;
		if (_currentDocument != null)
		{
			_currentDocument.TextChanged += TextArea_Document_TextChanged;
			DoSearch(false);
		}
	}

	private void UpdateSearch()
	{
		// only reset as long as there are results
		// if no results are found, the "no matches found" message should not flicker.
		// if results are found by the next run, the message will be hidden inside DoSearch ...
		try
		{
			if (_renderer.CurrentResults.Any() && (_messageView != null))
			{
				_messageView.IsVisible = false;
			}
			_strategy = SearchStrategyFactory.Create(SearchPattern ?? "", !MatchCase, WholeWords, UseRegex ? SearchMode.RegEx : SearchMode.Normal);
			OnSearchOptionsChanged(new SearchOptionsChangedEventArgs(SearchPattern, MatchCase, UseRegex, WholeWords));
			DoSearch(true);
		}
		catch (SearchPatternException)
		{
			CleanSearchResults();
			UpdateSearchLabel();
		}
	}

	private void UpdateSearchLabel()
	{
		if ((_messageView == null) || (_messageViewContent == null))
		{
			return;
		}

		_messageView.IsVisible = true;

		if (!_renderer.CurrentResults.Any())
		{
			_messageViewContent.Text = SR.SearchNoMatchesFoundText;
		}
		else
		{
			if (_currentSearchResultIndex == -1)
			{
				if (_renderer.CurrentResults.Count == 1)
				{
					_messageViewContent.Text = SR.Search1Match;
				}
				else
				{
					_messageViewContent.Text = string.Format(SR.SearchXMatches,
						_renderer.CurrentResults.Count);
				}
			}
			else
			{
				_messageViewContent.Text = string.Format(SR.SearchXOfY,
					_currentSearchResultIndex + 1,
					_renderer.CurrentResults.Count);
			}
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// Fired when SearchOptions are changed inside the SearchPanel.
	/// </summary>
	public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged;

	#endregion
}