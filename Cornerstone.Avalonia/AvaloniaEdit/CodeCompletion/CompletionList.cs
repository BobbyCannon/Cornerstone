#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;
using Cornerstone.Extensions;
using PropertyChanged;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;

/// <summary>
/// The listbox used inside the CompletionWindow, contains CompletionListBox.
/// </summary>
[DoNotNotify]
public class CompletionList : TemplatedControl
{
	#region Fields

	/// <summary>
	/// Dependency property for <see cref="EmptyTemplate" />.
	/// </summary>
	public static readonly StyledProperty<ControlTemplate> EmptyTemplateProperty =
		AvaloniaProperty.Register<CompletionList, ControlTemplate>(nameof(EmptyTemplate));

	private readonly ObservableCollection<ICompletionData> _completionData;
	private ObservableCollection<ICompletionData> _currentList;
	private CompletionListBox _listBox;

	/// <summary>
	/// This is all typed characters after the list is created.
	/// <see cref="SelectItem" /> gets called twice for every typed character (once from FormatLine),
	/// this helps execute <see cref="SelectItem" /> only once.
	/// </summary>
	private string _typedCharacters;

	#endregion

	#region Constructors

	public CompletionList(string prefix, IEnumerable<ICompletionData> suggestions, params Key[] completionsKeys)
	{
		Prefix = prefix;

		_completionData = new ObservableCollection<ICompletionData>(suggestions);

		CompletionAcceptKeys = completionsKeys?.Length > 0
			? completionsKeys
			: [Key.Enter, Key.Tab];

		AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble, true);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the array of keys that are supposed to request insertion of the completion
	/// </summary>
	public Key[] CompletionAcceptKeys { get; }

	/// <summary>
	/// Gets the list to which completion data can be added.
	/// </summary>
	public IList<ICompletionData> CompletionData => _completionData;

	/// <summary>
	/// The current list that has possibly been filtered.
	/// </summary>
	public IList<ICompletionData> CurrentList => ListBox?.Items.Cast<ICompletionData>().ToList()
		?? _currentList?.ToList()
		?? CompletionData;

	/// <summary>
	/// Content of EmptyTemplate will be shown when CompletionList contains no items.
	/// If EmptyTemplate is null, nothing will be shown.
	/// </summary>
	public ControlTemplate EmptyTemplate
	{
		get => GetValue(EmptyTemplateProperty);
		set => SetValue(EmptyTemplateProperty, value);
	}

	/// <summary>
	/// Gets the list box.
	/// </summary>
	public CompletionListBox ListBox
	{
		get
		{
			if (_listBox == null)
			{
				ApplyTemplate();
			}
			return _listBox;
		}
	}

	public string Prefix { get; }

	/// <summary>
	/// Gets the scroll viewer used in this list box.
	/// </summary>
	public ScrollViewer ScrollViewer => _listBox?.ScrollViewer;

	/// <summary>
	/// Gets/Sets the selected item.
	/// </summary>
	/// <remarks>
	/// The setter of this property does not scroll to the selected item.
	/// You might want to also call <see cref="ScrollIntoView" />.
	/// </remarks>
	public ICompletionData SelectedItem
	{
		get => _listBox?.SelectedItem as ICompletionData;
		set
		{
			if ((_listBox == null) && (value != null))
			{
				ApplyTemplate();
			}
			if (_listBox != null) // may still be null if ApplyTemplate fails, or if listBox and value both are null
			{
				_listBox.SelectedItem = value;
			}
		}
	}

	#endregion

	#region Methods

	public ObservableCollection<ICompletionData> FilterAndOrderList(string query)
	{
		var filter = string.IsNullOrEmpty(Prefix) ? query : Prefix + query;
		var currentFilter = string.IsNullOrEmpty(Prefix) ? _typedCharacters : Prefix + _typedCharacters;

		//Debug.WriteLine($"{nameof(FilterList)} {filter} : {query} {_prefix} / {_currentList}");

		// if the user just typed one more character, don't filter all data but just filter what we are already displaying
		var listToFilter = (_currentList != null)
			&& !string.IsNullOrEmpty(currentFilter)
			&& !string.IsNullOrEmpty(filter)
			&& filter.StartsWith(currentFilter, StringComparison.Ordinal)
				? _currentList
				: _completionData;

		var matchingItems = listToFilter
			.Select(x => new { item = x, quality = GetMatchQuality(x.DisplayText, filter, query) })
			.Where(x => x.quality > 0)
			.Select(x => new { Item = x.item, Quality = x.quality })
			.OrderBy(x => x.Quality)
			.ThenBy(x => x.Item.Priority)
			.ThenBy(x => x.Item.DisplayText)
			.Select(x => x.Item)
			.ToList();

		var response = new ObservableCollection<ICompletionData>(matchingItems);
		return response;
	}

	/// <summary>
	/// Handles a key press. Used to let the completion list handle key presses while the
	/// focus is still on the text editor.
	/// </summary>
	public void HandleKey(KeyEventArgs e)
	{
		// We have to do some key handling manually, because the default doesn't work with our simulated events.
		// Also, the default PageUp/PageDown implementation changes the focus, so we avoid it.
		switch (e.Key)
		{
			case Key.Down:
			{
				e.Handled = true;
				_listBox?.SelectIndex(_listBox.SelectedIndex + 1);
				break;
			}
			case Key.Up:
			{
				e.Handled = true;
				_listBox?.SelectIndex(_listBox.SelectedIndex - 1);
				break;
			}
			case Key.PageDown:
			{
				e.Handled = true;
				_listBox?.SelectIndex(_listBox.SelectedIndex + _listBox.VisibleItemCount);
				break;
			}
			case Key.PageUp:
			{
				e.Handled = true;
				_listBox?.SelectIndex(_listBox.SelectedIndex - _listBox.VisibleItemCount);
				break;
			}
			case Key.Home:
			{
				if (!e.KeyModifiers.HasAnyFlag(KeyModifiers.Shift, KeyModifiers.Alt, KeyModifiers.Control))
				{
					e.Handled = true;
					_listBox?.SelectIndex(0);
				}
				break;
			}
			case Key.End:
			{
				if (!e.KeyModifiers.HasAnyFlag(KeyModifiers.Shift, KeyModifiers.Alt, KeyModifiers.Control))
				{
					e.Handled = true;
					_listBox?.SelectIndex(_listBox.Items.Count - 1);
				}
				break;
			}
			default:
			{
				if (CompletionAcceptKeys.Contains(e.Key))
				{
					e.Handled = true;
					RequestInsertion(e);
				}
				break;
			}
		}
	}

	/// <summary>
	/// Raises the InsertionRequested event.
	/// </summary>
	public void RequestInsertion(EventArgs e)
	{
		InsertionRequested?.Invoke(this, e);
	}

	/// <summary>
	/// Scrolls the specified item into view.
	/// </summary>
	public void ScrollIntoView(ICompletionData item)
	{
		if (_listBox == null)
		{
			ApplyTemplate();
		}
		_listBox?.ScrollIntoView(item);
	}

	/// <summary>
	/// Selects the best match and filter the items.
	/// </summary>
	public void SelectItem(string text)
	{
		if (text == _typedCharacters)
		{
			return;
		}
		if (_listBox == null)
		{
			ApplyTemplate();
		}

		SelectItemFiltering(text);

		_typedCharacters = text;

		// Debug.WriteLine(_typedCharacters);
	}

	/// <summary>
	/// Filters CompletionList items to show only those matching given query, and selects the best match.
	/// </summary>
	public void SelectItemFiltering(string query)
	{
		if (_listBox == null)
		{
			return;
		}

		// Debug.WriteLine($"Query: {query}  Prefix: {Prefix}");

		var listBoxItems = FilterAndOrderList(query);
		_currentList = listBoxItems;
		_listBox.ItemsSource = listBoxItems;

		SelectIndexCentered(0);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		_listBox = e.NameScope.Find("PART_ListBox") as CompletionListBox;

		if (_listBox != null)
		{
			_listBox.ItemsSource = _completionData;
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (!e.Handled)
		{
			HandleKey(e);
		}
	}

	private decimal GetMatchQuality(string itemText, string query, string otherQuery)
	{
		if (itemText == null)
		{
			return -1;
		}

		// Qualities: (lower better)
		//  	1 = full match case-sensitive
		// 		2 = full match
		// 		3 = match start case-sensitive
		//		4 = match start
		//		5 = match CamelCase when length of query is 1 or 2 characters
		// 		6 = match substring case-sensitive
		//		7 = match substring
		//		8 = match CamelCase
		//		-1 = no match
		if (query == itemText)
		{
			return 1;
		}
		if (string.Equals(itemText, query, StringComparison.CurrentCultureIgnoreCase))
		{
			return 2;
		}

		if (itemText.StartsWith(query, StringComparison.CurrentCulture))
		{
			return 3;
		}
		if (itemText.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
		{
			return 4;
		}

		bool? camelCaseMatch = null;

		if (query.Length <= 2)
		{
			camelCaseMatch = itemText.CamelCaseMatch(query);
			if (camelCaseMatch == true)
			{
				return 5;
			}
		}

		decimal index;

		if ((index = itemText.IndexOf(query, StringComparison.CurrentCulture)) >= 0)
		{
			return 6 + (index / 1000m);
		}

		if ((index = itemText.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)) >= 0)
		{
			return 7 + (index / 1000m);
		}

		camelCaseMatch ??= itemText.CamelCaseMatch(query);

		if (camelCaseMatch == true)
		{
			return 8;
		}

		return !string.IsNullOrWhiteSpace(otherQuery)
			? GetMatchQuality(itemText, otherQuery, null)
			: -1;
	}

	private void OnPointerPressed(object sender, PointerPressedEventArgs e)
	{
		var visual = e.Source as Visual;
		if (!e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
		{
			return;
		}

		var listBoxItem = visual.VisualAncestorsAndSelf()
			.TakeWhile(v => v != this)
			.OfType<ListBoxItem>()
			.FirstOrDefault();

		if (listBoxItem != null)
		{
			// A completion item was clicked.
			RequestInsertion(e);
		}
	}

	private void SelectIndexCentered(int bestIndex)
	{
		if (bestIndex < 0)
		{
			_listBox.ClearSelection();
		}
		else
		{
			var firstItem = _listBox.FirstVisibleItem;
			if ((bestIndex < firstItem) || ((firstItem + _listBox.VisibleItemCount) <= bestIndex))
			{
				// CenterViewOn does nothing as CompletionListBox.ScrollViewer is null
				_listBox.CenterViewOn(bestIndex);
				_listBox.SelectIndex(bestIndex);
			}
			else
			{
				_listBox.SelectIndex(bestIndex);
			}
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// Is raised when the completion list indicates that the user has chosen
	/// an entry to be completed.
	/// </summary>
	public event EventHandler InsertionRequested;

	/// <summary>
	/// Occurs when the SelectedItem property changes.
	/// </summary>
	public event EventHandler<SelectionChangedEventArgs> SelectionChanged
	{
		add => AddHandler(SelectingItemsControl.SelectionChangedEvent, value);
		remove => RemoveHandler(SelectingItemsControl.SelectionChangedEvent, value);
	}

	#endregion
}