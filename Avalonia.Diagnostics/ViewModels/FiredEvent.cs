#region References

using System;
using System.Collections.ObjectModel;
using Avalonia.Diagnostics.Models;
using Avalonia.Interactivity;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class FiredEvent : ViewModel
{
	#region Fields

	private readonly RoutedEventArgs _eventArgs;
	private EventChainLink _handledBy;

	#endregion

	#region Constructors

	public FiredEvent(RoutedEventArgs eventArgs, EventChainLink originator, DateTime triggerTime)
	{
		_eventArgs = eventArgs ?? throw new ArgumentNullException(nameof(eventArgs));
		Originator = originator ?? throw new ArgumentNullException(nameof(originator));
		Event = _eventArgs.RoutedEvent;
		AddToChain(originator);
		TriggerTime = triggerTime;
	}

	#endregion

	#region Properties

	public string DisplayText
	{
		get
		{
			if (IsHandled)
			{
				return $"{Event.Name} on {Originator.HandlerName};" + Environment.NewLine +
					$"strategies: {Event.RoutingStrategies}; handled by: {HandledBy!.HandlerName}";
			}

			return $"{Event.Name} on {Originator.HandlerName}; strategies: {Event.RoutingStrategies}";
		}
	}

	public RoutedEvent Event { get; }

	public ObservableCollection<EventChainLink> EventChain { get; } = [];

	public EventChainLink HandledBy
	{
		get => _handledBy;
		set
		{
			if (_handledBy != value)
			{
				_handledBy = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsHandled));
				OnPropertyChanged(nameof(DisplayText));
			}
		}
	}

	public bool IsHandled => HandledBy?.Handled == true;

	public EventChainLink Originator { get; }

	public DateTime TriggerTime { get; }

	#endregion

	#region Methods

	public void AddToChain(EventChainLink link)
	{
		if (EventChain.Count > 0)
		{
			var prevLink = EventChain[EventChain.Count - 1];

			if (prevLink.Route != link.Route)
			{
				link.BeginsNewRoute = true;
			}
		}

		EventChain.Add(link);

		if ((HandledBy == null) && link.Handled)
		{
			HandledBy = link;
		}
	}

	public bool IsPartOfSameEventChain(RoutedEventArgs e)
	{
		// Note, Avalonia might reuse RoutedEventArgs for different events to avoid extra allocations.
		// Like, PointerEntered and PointerExited will use the same instance of RoutedEventArgs. 
		return (e == _eventArgs) && (e.RoutedEvent == Event);
	}

	#endregion
}