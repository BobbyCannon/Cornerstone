#region References

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.TextEditor.CodeCompletion;

/// <summary>
/// The listbox used inside the CompletionWindow, contains CompletionListBox.
/// </summary>
[DoNotNotify]
public class CompletionList : TemplatedControl
{
	private readonly SpeedyList<ICompletionData> _completionData;

	#region Fields

	private CompletionListBox _listBox;

	#endregion

	#region Constructors

	public CompletionList(ICompletionProvider provider)
		: this(provider.GetTextToReplace(), provider.Data)
	{

	}
	public CompletionList(string prefix, SpeedyList<ICompletionData> completionData)
	{
		_completionData = completionData;

		Prefix = prefix;
		CompletionData = _completionData;
		CompletionAcceptKeys = [Key.Enter, Key.Tab];

		AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble, true);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the array of keys that are supposed to request insertion of the completion
	/// </summary>
	public Key[] CompletionAcceptKeys { get; }

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

	public SpeedyList<ICompletionData> CompletionData { get; }

	/// <summary>
	/// This is all typed characters after the list is created.
	/// <see cref="SetFilter" /> gets called twice for every typed character (once from FormatLine),
	/// this helps execute <see cref="SetFilter" /> only once.
	/// </summary>
	public string CurrentFilter { get; private set; }

	#endregion

	#region Methods

	public void FilterAndOrderList()
	{
		if (string.IsNullOrWhiteSpace(CurrentFilter))
		{
			CompletionData.Load(_completionData);
			return;
		}

		var fullFilter = string.IsNullOrEmpty(Prefix) ? CurrentFilter : Prefix + CurrentFilter;

		foreach (var item in _completionData)
		{
			item.Priority = GetMatchQuality(item.DisplayText, fullFilter, CurrentFilter);

			//Debug.WriteLine($"{nameof(FilterAndOrderList)} {item.DisplayText} - {fullFilter} / {fullFilter} - {item.Priority}");
		}

		var filteredAndOrdered = _completionData
			.Where(x => x.Priority < 100)
			.OrderBy(x => x.Priority)
			.ToList();

		CompletionData.Load(filteredAndOrdered);
	}

	public static decimal GetMatchQuality(string itemText, string fullFilter, string typedFilter)
	{
		if (itemText == null)
		{
			return 100;
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
		//		100 = no match
		if (fullFilter == itemText)
		{
			return 1;
		}
		if (string.Equals(itemText, fullFilter, StringComparison.CurrentCultureIgnoreCase))
		{
			return 2;
		}

		if (itemText.StartsWith(fullFilter, StringComparison.CurrentCulture))
		{
			return 3;
		}
		if (itemText.StartsWith(fullFilter, StringComparison.CurrentCultureIgnoreCase))
		{
			return 4;
		}

		bool? camelCaseMatch = null;

		if (fullFilter.Length <= 2)
		{
			camelCaseMatch = itemText.CamelCaseMatch(fullFilter);
			if (camelCaseMatch == true)
			{
				return 5;
			}
		}

		decimal index;

		if ((index = itemText.IndexOf(fullFilter, StringComparison.CurrentCulture)) >= 0)
		{
			return 6 + (index / 1000m);
		}

		if ((index = itemText.IndexOf(fullFilter, StringComparison.CurrentCultureIgnoreCase)) >= 0)
		{
			return 7 + (index / 1000m);
		}

		camelCaseMatch ??= itemText.CamelCaseMatch(fullFilter);

		if (camelCaseMatch == true)
		{
			return 8;
		}

		return !string.IsNullOrWhiteSpace(typedFilter)
			? GetMatchQuality(itemText, typedFilter, null)
			: 100;
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
	public void SetFilter(string filter)
	{
		if (filter == CurrentFilter)
		{
			return;
		}
		if (_listBox == null)
		{
			ApplyTemplate();
		}

		CurrentFilter = filter;

		FilterAndOrderList();
		SelectIndexCentered(0);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		_listBox = e.NameScope.Find("PART_ListBox") as CompletionListBox;

		if (_listBox != null)
		{
			_listBox.ItemsSource = CompletionData;
			_listBox.SelectedItem = _completionData.FirstOrDefault();
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
		var listBox = _listBox;
		if (listBox == null)
		{
			return;
		}

		if (bestIndex < 0)
		{
			listBox.ClearSelection();
		}
		else
		{
			var firstItem = listBox.FirstVisibleItem;
			if ((bestIndex < firstItem) || ((firstItem + listBox.VisibleItemCount) <= bestIndex))
			{
				// CenterViewOn does nothing as CompletionListBox.ScrollViewer is null
				listBox.CenterViewOn(bestIndex);
				listBox.SelectIndex(bestIndex);
			}
			else
			{
				listBox.SelectIndex(bestIndex);
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