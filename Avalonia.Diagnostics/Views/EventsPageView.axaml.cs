#region References

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.Controls;
using Avalonia.Diagnostics.Models;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

#endregion

namespace Avalonia.Diagnostics.Views;

public partial class EventsPageView : UserControl
{
	#region Fields

	private IDisposable _adorner;
	private readonly ListBox _events;

	#endregion

	#region Constructors

	public EventsPageView()
	{
		InitializeComponent();
		_events = this.GetControl<ListBox>("EventsList");
	}

	#endregion

	#region Methods

	public void NavigateTo(object sender, TappedEventArgs e)
	{
		if (DataContext is EventsPageViewModel vm && sender is Control control)
		{
			switch (control.Tag)
			{
				case EventChainLink chainLink:
				{
					vm.RequestTreeNavigateTo(chainLink);
					break;
				}
				case RoutedEvent evt:
				{
					vm.SelectEventByType(evt);

					break;
				}
			}
		}
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);

		if (DataContext is EventsPageViewModel vm)
		{
			vm.RecordedEvents.CollectionChanged += OnRecordedEventsChanged;
		}
	}

	private void ListBoxItem_PointerEntered(object sender, PointerEventArgs e)
	{
		if (DataContext is EventsPageViewModel vm
			&& sender is Control control
			&& control.DataContext is EventChainLink chainLink
			&& chainLink.Handler is Visual visual)
		{
			_adorner = ControlHighlightAdorner.Add(visual, vm.MainView.ShouldVisualizeMarginPadding);
		}
	}

	private void ListBoxItem_PointerExited(object sender, PointerEventArgs e)
	{
		_adorner?.Dispose();
	}

	private void OnRecordedEventsChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (sender is ObservableCollection<FiredEvent> events)
		{
			var evt = events.LastOrDefault();

			if (evt is null)
			{
				return;
			}

			Dispatcher.UIThread.Post(() => _events.ScrollIntoView(evt));
		}
	}

	#endregion
}