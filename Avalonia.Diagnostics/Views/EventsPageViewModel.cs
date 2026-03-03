#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.Models;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Interactivity;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Diagnostics.Views;

public class EventsPageViewModel : ViewModel
{
	#region Fields

	private FiredEvent _selectedEvent;
	private EventTreeNodeBase _selectedNode;

	private static readonly HashSet<RoutedEvent> s_defaultEvents =
	[
		Button.ClickEvent,
		InputElement.KeyDownEvent,
		InputElement.KeyUpEvent,
		InputElement.TextInputEvent,
		InputElement.PointerReleasedEvent,
		InputElement.PointerPressedEvent
	];

	#endregion

	#region Constructors

	public EventsPageViewModel(MainViewModel mainViewModel)
	{
		MainView = mainViewModel;

		Nodes = RoutedEventRegistry.Instance.GetAllRegistered()
			.GroupBy(e => e.OwnerType)
			.OrderBy(e => e.Key.Name)
			.Select(g => new EventOwnerTreeNode(g.Key, g, this))
			.ToArray();

		EventsFilter = new FilterViewModel();
		EventsFilter.RefreshFilter += (s, e) => UpdateEventFilters();

		EnableDefault();
	}

	#endregion

	#region Properties

	public FilterViewModel EventsFilter { get; }

	public MainViewModel MainView { get; }

	public string Name => "Events";

	public EventTreeNodeBase[] Nodes { get; }

	public ObservableCollection<FiredEvent> RecordedEvents { get; } = [];

	public FiredEvent SelectedEvent
	{
		get => _selectedEvent;
		set => SetProperty(ref _selectedEvent, value);
	}

	public EventTreeNodeBase SelectedNode
	{
		get => _selectedNode;
		set => SetProperty(ref _selectedNode, value);
	}

	#endregion

	#region Methods

	public void Clear()
	{
		RecordedEvents.Clear();
	}

	public void DisableAll()
	{
		EvaluateNodeEnabled(_ => false);
	}

	public void EnableDefault()
	{
		EvaluateNodeEnabled(node => s_defaultEvents.Contains(node.Event));
	}

	public void RequestTreeNavigateTo(EventChainLink navTarget)
	{
		if (navTarget.Handler is Control control)
		{
			MainView.RequestTreeNavigateTo(control, true);
		}
	}

	public void SelectEventByType(RoutedEvent evt)
	{
		foreach (var node in Nodes)
		{
			var result = FindNode(node, evt);

			if (result != null && result.IsVisible)
			{
				SelectedNode = result;

				break;
			}
		}

		static EventTreeNodeBase FindNode(EventTreeNodeBase node, RoutedEvent eventType)
		{
			if (node is EventTreeNode eventNode && eventNode.Event == eventType)
			{
				return node;
			}

			if (node.Children != null)
			{
				foreach (var child in node.Children)
				{
					var result = FindNode(child, eventType);

					if (result != null)
					{
						return result;
					}
				}
			}

			return null;
		}
	}

	private void EvaluateNodeEnabled(Func<EventTreeNode, bool> eval)
	{
		void ProcessNode(EventTreeNodeBase node)
		{
			if (node is EventTreeNode eventNode)
			{
				node.IsEnabled = eval(eventNode);
			}

			if (node.Children != null)
			{
				foreach (var childNode in node.Children)
				{
					ProcessNode(childNode);
				}
			}
		}

		foreach (var node in Nodes)
		{
			ProcessNode(node);
		}
	}

	private void UpdateEventFilters()
	{
		foreach (var node in Nodes)
		{
			FilterNode(node, false);
		}

		bool FilterNode(EventTreeNodeBase node, bool isParentVisible)
		{
			var matchesFilter = EventsFilter.Filter(node.Text);
			var hasVisibleChild = false;

			if (node.Children != null)
			{
				foreach (var childNode in node.Children)
				{
					hasVisibleChild |= FilterNode(childNode, matchesFilter);
				}
			}

			node.IsVisible = hasVisibleChild || matchesFilter || isParentVisible;

			return node.IsVisible;
		}
	}

	#endregion
}