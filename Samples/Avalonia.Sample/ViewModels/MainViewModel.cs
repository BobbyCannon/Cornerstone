#region References

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Sample.Tabs;
using Cornerstone.Avalonia;
using Cornerstone.Location;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Timer = System.Timers.Timer;

#endregion

namespace Avalonia.Sample.ViewModels;

public class MainViewModel : ViewModel
{
	#region Fields

	private readonly ThrottleService _saveThrottle;

	private readonly Timer _themeCycleTimer;

	#endregion

	#region Constructors

	public MainViewModel(ApplicationSettings applicationSettings,
		ILocationProvider locationProvider,
		IRuntimeInformation runtimeInformation,
		IDispatcher dispatcher) : base(dispatcher)
	{
		ApplicationSettings = applicationSettings;
		LocationProvider = locationProvider;
		RuntimeInformation = runtimeInformation;

		_themeCycleTimer = new Timer(1000);
		_themeCycleTimer.Elapsed += OnThemeCycleTimerElapsed;
		_saveThrottle = new ThrottleService(TimeSpan.FromSeconds(1), ThrottledSave);

		if (ApplicationSettings.CycleThemes)
		{
			_themeCycleTimer.Start();
		}

		Tabs =
		[
			new TabItemViewModel(TabThemes.HeaderName, new TabThemes(this)),
			new TabItemViewModel(TabAdornerLayer.HeaderName, new TabAdornerLayer()),
			new TabItemViewModel(TabAutoCompleteBox.HeaderName, new TabAutoCompleteBox()),
			new TabItemViewModel(TabBrowser.HeaderName, new TabBrowser()),
			new TabItemViewModel(TabButton.HeaderName, new TabButton()),
			new TabItemViewModel(TabButtonSpinner.HeaderName, new TabButtonSpinner()),
			new TabItemViewModel(TabCalendar.HeaderName, new TabCalendar()),
			new TabItemViewModel(TabCalendarDatePicker.HeaderName, new TabCalendarDatePicker()),
			new TabItemViewModel(TabCarousel.HeaderName, new TabCarousel()),
			new TabItemViewModel(TabCheckBox.HeaderName, new TabCheckBox()),
			new TabItemViewModel(TabCircularProgress.HeaderName, new TabCircularProgress()),
			new TabItemViewModel(TabComboBox.HeaderName, new TabComboBox()),
			new TabItemViewModel(TabContextMenu.HeaderName, new TabContextMenu()),
			new TabItemViewModel(TabDataGrid.HeaderName, new TabDataGrid()),
			new TabItemViewModel(TabDateTimePicker.HeaderName, new TabDateTimePicker()),
			new TabItemViewModel(TabDebounceAndThrottle.HeaderName, new TabDebounceAndThrottle()),
			new TabItemViewModel(TabExpander.HeaderName, new TabExpander()),
			new TabItemViewModel(TabFlyout.HeaderName, new TabFlyout()),
			new TabItemViewModel(TabGridSplitter.HeaderName, new TabGridSplitter()),
			new TabItemViewModel(TabIcons.HeaderName, new TabIcons()),
			new TabItemViewModel(TabListBox.HeaderName, new TabListBox()),
			new TabItemViewModel(TabLocationProvider.HeaderName, new TabLocationProvider()),
			new TabItemViewModel(TabMapsui.HeaderName, new TabMapsui()),
			new TabItemViewModel(TabNotificationCard.HeaderName, new TabNotificationCard()),
			new TabItemViewModel(TabNumericUpDown.HeaderName, new TabNumericUpDown()),
			new TabItemViewModel(TabProgressBar.HeaderName, new TabProgressBar()),
			new TabItemViewModel(TabRadioButton.HeaderName, new TabRadioButton()),
			new TabItemViewModel(TabScrollViewer.HeaderName, new TabScrollViewer()),
			new TabItemViewModel(TabSlider.HeaderName, new TabSlider()),
			new TabItemViewModel(TabSpeedyList.HeaderName, new TabSpeedyList()),
			new TabItemViewModel(TabTabControl.HeaderName, new TabTabControl()),
			new TabItemViewModel(TabTextBox.HeaderName, new TabTextBox()),
			new TabItemViewModel(TabTextEditor.HeaderName, new TabTextEditor()),
			new TabItemViewModel(TabToggleSwitch.HeaderName, new TabToggleSwitch()),
			new TabItemViewModel(TabToolTip.HeaderName, new TabToolTip()),
			new TabItemViewModel(TabTreeView.HeaderName, new TabTreeView())
		];

		var tab = Tabs.FirstOrDefault(x => x.Header == ApplicationSettings.SelectedTabName);
		if (tab != null)
		{
			SelectedTab = tab;
		}
	}

	#endregion

	#region Properties

	public ApplicationSettings ApplicationSettings { get; }

	public ILocationProvider LocationProvider { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	public TabItemViewModel SelectedTab { get; set; }

	public ObservableCollection<TabItemViewModel> Tabs { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Initialize()
	{
		ApplicationSettings.PropertyChanged += ApplicationSettingsOnPropertyChanged;
		base.Initialize();
	}

	/// <inheritdoc />
	public override void Uninitialize()
	{
		ApplicationSettings.PropertyChanged -= ApplicationSettingsOnPropertyChanged;
		base.Uninitialize();
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(string propertyName = null)
	{
		switch (propertyName)
		{
			case nameof(SelectedTab):
			{
				if (RuntimeInformation.IsLoaded)
				{
					ApplicationSettings.SelectedTabName = SelectedTab.Header;
				}
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	private void ApplicationSettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ApplicationSettings.CycleThemes):
			{
				if (ApplicationSettings.CycleThemes)
				{
					_themeCycleTimer.Start();
				}
				else
				{
					_themeCycleTimer.Stop();
				}
				break;
			}
		}

		_saveThrottle.Trigger();
	}

	private void OnThemeCycleTimerElapsed(object sender, ElapsedEventArgs args)
	{
		this.Dispatch(() => { ApplicationSettings.ThemeColor = Theme.GetNextThemeColor(ApplicationSettings.ThemeColor); });
	}

	private void ThrottledSave(CancellationToken obj)
	{
		ApplicationSettings.Save();
	}

	#endregion
}

public class TabItemViewModel
{
	#region Constructors

	public TabItemViewModel(string header, UserControl content)
	{
		Header = header;
		Content = content;
	}

	#endregion

	#region Properties

	public UserControl Content { get; }
	public string Header { get; }

	#endregion
}