#region References

using System;
using Avalonia.Diagnostics.Models;
using Avalonia.Diagnostics.Views;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using Avalonia.Threading;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class EventTreeNode : EventTreeNodeBase
{
	#region Fields

	private FiredEvent _currentEvent;
	private bool _isRegistered;
	private readonly EventsPageViewModel _parentViewModel;

	#endregion

	#region Constructors

	public EventTreeNode(EventOwnerTreeNode parent, RoutedEvent @event, EventsPageViewModel vm)
		: base(parent, @event.Name)
	{
		Event = @event ?? throw new ArgumentNullException(nameof(@event));
		_parentViewModel = vm ?? throw new ArgumentNullException(nameof(vm));
	}

	#endregion

	#region Properties

	public RoutedEvent Event { get; }

	public override bool? IsEnabled
	{
		get => base.IsEnabled;
		set
		{
			if (base.IsEnabled != value)
			{
				base.IsEnabled = value;
				UpdateTracker();
				if ((Parent != null) && _updateParent)
				{
					try
					{
						Parent._updateChildren = false;
						Parent.UpdateChecked();
					}
					finally
					{
						Parent._updateChildren = true;
					}
				}
			}
		}
	}

	#endregion

	#region Methods

	private void HandleEvent(object sender, RoutedEventArgs e)
	{
		if (!_isRegistered || (IsEnabled == false))
		{
			return;
		}
		if (sender is Visual v && v.DoesBelongToDevTool())
		{
			return;
		}

		var s = sender!;
		var handled = e.Handled;
		var route = e.Route;
		var triggerTime = DateTime.Now;

		void handler()
		{
			if ((_currentEvent == null) || !_currentEvent.IsPartOfSameEventChain(e))
			{
				_currentEvent = new FiredEvent(e, new EventChainLink(s, handled, route), triggerTime);

				_parentViewModel.RecordedEvents.Add(_currentEvent);

				while (_parentViewModel.RecordedEvents.Count > 100)
				{
					_parentViewModel.RecordedEvents.RemoveAt(0);
				}
			}
			else
			{
				_currentEvent.AddToChain(new EventChainLink(s, handled, route));
			}
		}

		;

		if (!Dispatcher.UIThread.CheckAccess())
		{
			Dispatcher.UIThread.Post(handler);
		}
		else
		{
			handler();
		}
	}

	private void HandleRouteFinished(RoutedEventArgs e)
	{
		if (!_isRegistered || (IsEnabled == false))
		{
			return;
		}
		if (e.Source is Visual v && v.DoesBelongToDevTool())
		{
			return;
		}

		var s = e.Source;
		var handled = e.Handled;
		var route = e.Route;

		void handler()
		{
			if ((_currentEvent != null) && handled)
			{
				var linkIndex = _currentEvent.EventChain.Count - 1;
				var link = _currentEvent.EventChain[linkIndex];

				link.Handled = true;
				_currentEvent.HandledBy ??= link;
			}
		}

		if (!Dispatcher.UIThread.CheckAccess())
		{
			Dispatcher.UIThread.Post(handler);
		}
		else
		{
			handler();
		}
	}

	private void UpdateTracker()
	{
		if (IsEnabled.GetValueOrDefault() && !_isRegistered)
		{
			var allRoutes = RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble;

			// FIXME: This leaks event handlers.
			Event.AddClassHandler(typeof(object), HandleEvent, allRoutes, true);
			Event.RouteFinished.Subscribe(HandleRouteFinished);

			_isRegistered = true;
		}
	}

	#endregion
}