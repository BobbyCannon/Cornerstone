#region References

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
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

	public MainViewModel(ApplicationSettings applicationSettings, ILocationProvider locationProvider, IDateTimeProvider timeProvider,
		IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
		: base(dispatcher)
	{
		ApplicationSettings = applicationSettings;
		LocationProvider = locationProvider;
		TimeProvider = timeProvider;
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
			new TabItemViewModel(TabComboBox.HeaderName, new TabComboBox(dispatcher)),
			new TabItemViewModel(TabConnectionStringBuilder.HeaderName, new TabConnectionStringBuilder()),
			new TabItemViewModel(TabDatabases.HeaderName, new TabDatabases()),
			new TabItemViewModel(TabDataGrid.HeaderName, new TabDataGrid()),
			new TabItemViewModel(TabDateTimePicker.HeaderName, new TabDateTimePicker()),
			new TabItemViewModel(TabDebounceAndThrottle.HeaderName, new TabDebounceAndThrottle(timeProvider, dispatcher)),
			new TabItemViewModel(TabDockingManager.HeaderName, new TabDockingManager()),
			new TabItemViewModel(TabExpander.HeaderName, new TabExpander()),
			new TabItemViewModel(TabFlyout.HeaderName, new TabFlyout()),
			new TabItemViewModel(TabGridSplitter.HeaderName, new TabGridSplitter()),
			new TabItemViewModel(TabIcons.HeaderName, new TabIcons()),
			new TabItemViewModel(TabInputs.HeaderName, new TabInputs()),
			new TabItemViewModel(TabListBox.HeaderName, new TabListBox()),
			new TabItemViewModel(TabLocationProvider.HeaderName, new TabLocationProvider()),
			new TabItemViewModel(TabMapsui.HeaderName, new TabMapsui()),
			new TabItemViewModel(TabMenu.HeaderName, new TabMenu()),
			new TabItemViewModel(TabNmea.HeaderName, new TabNmea()),
			new TabItemViewModel(TabNotificationCard.HeaderName, new TabNotificationCard()),
			new TabItemViewModel(TabNumericUpDown.HeaderName, new TabNumericUpDown()),
			new TabItemViewModel(TabProfiling.HeaderName, new TabProfiling(this)),
			new TabItemViewModel(TabProgressBar.HeaderName, new TabProgressBar()),
			new TabItemViewModel(TabRadioButton.HeaderName, new TabRadioButton()),
			new TabItemViewModel(TabRelayCommand.HeaderName, new TabRelayCommand()),
			new TabItemViewModel(TabRuntimeInformation.HeaderName, new TabRuntimeInformation(this)),
			new TabItemViewModel(TabScrollViewer.HeaderName, new TabScrollViewer()),
			new TabItemViewModel(TabSlider.HeaderName, new TabSlider()),
			new TabItemViewModel(TabSpeedyList.HeaderName, new TabSpeedyList()),
			new TabItemViewModel(TabSyncManager.HeaderName, new TabSyncManager(timeProvider, runtimeInformation, dispatcher)),
			new TabItemViewModel(TabTabControl.HeaderName, new TabTabControl()),
			new TabItemViewModel(TabTextBox.HeaderName, new TabTextBox()),
			new TabItemViewModel(TabTextEditor.HeaderName, new TabTextEditor()),
			new TabItemViewModel(TabThemes.HeaderName, new TabThemes(this, dispatcher)),
			new TabItemViewModel(TabThemeVariantScope.HeaderName, new TabThemeVariantScope()),
			new TabItemViewModel(TabToggleButton.HeaderName, new TabToggleButton()),
			new TabItemViewModel(TabToggleSwitch.HeaderName, new TabToggleSwitch()),
			new TabItemViewModel(TabToolTip.HeaderName, new TabToolTip()),
			new TabItemViewModel(TabTreeView.HeaderName, new TabTreeView()),
			new TabItemViewModel(TabWeakEvents.HeaderName, new TabWeakEvents())
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

	public IDateTimeProvider TimeProvider { get; }

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
		_themeCycleTimer.Enabled = false;
		_themeCycleTimer.Close();

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