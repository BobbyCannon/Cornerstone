#region References

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Diagnostics.Controls;
using Cornerstone.Presentation.Diagnostics.Models;
using Cornerstone.Presentation.Diagnostics.ViewModels;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Diagnostics.Views;

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

			Dispatcher.Post(() => _events.ScrollIntoView(evt));
		}
	}

	#endregion
}