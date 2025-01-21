#region References

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Location;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Tabs;
using Timer = System.Timers.Timer;

#endregion

namespace Cornerstone.Sample.ViewModels;

public class MainViewModel : ViewModel
{
	#region Fields

	private readonly ThrottleService _saveThrottle;
	private readonly Timer _themeCycleTimer;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public MainViewModel(
		ApplicationSettings applicationSettings,
		IDependencyProvider dependencyProvider,
		ILocationProvider locationProvider,
		IDateTimeProvider timeProvider,
		IRuntimeInformation runtimeInformation,
		IDispatcher dispatcher)
		: base(dispatcher)
	{
		ApplicationSettings = applicationSettings;
		DependencyProvider = dependencyProvider;
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
			new TabItemViewModel(TabAdornerLayer.HeaderName, DependencyProvider.GetInstance<TabAdornerLayer>()),
			new TabItemViewModel(TabAutoCompleteBox.HeaderName, DependencyProvider.GetInstance<TabAutoCompleteBox>()),
			new TabItemViewModel(TabBrowser.HeaderName, DependencyProvider.GetInstance<TabBrowser>()),
			new TabItemViewModel(TabButton.HeaderName, DependencyProvider.GetInstance<TabButton>()),
			new TabItemViewModel(TabButtonSpinner.HeaderName, DependencyProvider.GetInstance<TabButtonSpinner>()),
			new TabItemViewModel(TabCalendar.HeaderName, DependencyProvider.GetInstance<TabCalendar>()),
			new TabItemViewModel(TabCalendarDatePicker.HeaderName, DependencyProvider.GetInstance<TabCalendarDatePicker>()),
			new TabItemViewModel(TabCarousel.HeaderName, DependencyProvider.GetInstance<TabCarousel>()),
			new TabItemViewModel(TabCheckBox.HeaderName, DependencyProvider.GetInstance<TabCheckBox>()),
			new TabItemViewModel(TabCircularProgress.HeaderName, DependencyProvider.GetInstance<TabCircularProgress>()),
			new TabItemViewModel(TabComboBox.HeaderName, DependencyProvider.GetInstance<TabComboBox>()),
			new TabItemViewModel(TabConnectionStringBuilder.HeaderName, DependencyProvider.GetInstance<TabConnectionStringBuilder>()),
			new TabItemViewModel(TabCredentialVault.HeaderName, DependencyProvider.GetInstance<TabCredentialVault>()),
			new TabItemViewModel(TabDatabases.HeaderName, DependencyProvider.GetInstance<TabDatabases>()),
			new TabItemViewModel(TabDataGrid.HeaderName, DependencyProvider.GetInstance<TabDataGrid>()),
			new TabItemViewModel(TabDateTimePicker.HeaderName, DependencyProvider.GetInstance<TabDateTimePicker>()),
			new TabItemViewModel(TabDebounceAndThrottle.HeaderName, DependencyProvider.GetInstance<TabDebounceAndThrottle>()),
			new TabItemViewModel(TabDockingManager.HeaderName, DependencyProvider.GetInstance<TabDockingManager>()),
			new TabItemViewModel(TabExpander.HeaderName, DependencyProvider.GetInstance<TabExpander>()),
			new TabItemViewModel(TabFlyout.HeaderName, DependencyProvider.GetInstance<TabFlyout>()),
			new TabItemViewModel(TabGridSplitter.HeaderName, DependencyProvider.GetInstance<TabGridSplitter>()),
			new TabItemViewModel(TabIcons.HeaderName, DependencyProvider.GetInstance<TabIcons>()),
			new TabItemViewModel(TabInputs.HeaderName, DependencyProvider.GetInstance<TabInputs>()),
			new TabItemViewModel(TabListBox.HeaderName, DependencyProvider.GetInstance<TabListBox>()),
			new TabItemViewModel(TabLocationProvider.HeaderName, DependencyProvider.GetInstance<TabLocationProvider>()),
			new TabItemViewModel(TabMapsui.HeaderName, DependencyProvider.GetInstance<TabMapsui>()),
			new TabItemViewModel(TabMenu.HeaderName, DependencyProvider.GetInstance<TabMenu>()),
			new TabItemViewModel(TabNmea.HeaderName, DependencyProvider.GetInstance<TabNmea>()),
			new TabItemViewModel(TabNotificationCard.HeaderName, DependencyProvider.GetInstance<TabNotificationCard>()),
			new TabItemViewModel(TabNumericUpDown.HeaderName, DependencyProvider.GetInstance<TabNumericUpDown>()),
			new TabItemViewModel(TabProfiling.HeaderName, DependencyProvider.GetInstance<TabProfiling>()),
			new TabItemViewModel(TabProgressBar.HeaderName, DependencyProvider.GetInstance<TabProgressBar>()),
			new TabItemViewModel(TabPropertyGrid.HeaderName, DependencyProvider.GetInstance<TabPropertyGrid>()),
			new TabItemViewModel(TabRadioButton.HeaderName, DependencyProvider.GetInstance<TabRadioButton>()),
			new TabItemViewModel(TabRelayCommand.HeaderName, DependencyProvider.GetInstance<TabRelayCommand>()),
			new TabItemViewModel(TabRuntimeInformation.HeaderName, DependencyProvider.GetInstance<TabRuntimeInformation>()),
			new TabItemViewModel(TabScrollViewer.HeaderName, DependencyProvider.GetInstance<TabScrollViewer>()),
			new TabItemViewModel(TabSecurityKeys.HeaderName, DependencyProvider.GetInstance<TabSecurityKeys>()),
			new TabItemViewModel(TabSlider.HeaderName, DependencyProvider.GetInstance<TabSlider>()),
			new TabItemViewModel(TabSpeedyList.HeaderName, DependencyProvider.GetInstance<TabSpeedyList>()),
			new TabItemViewModel(TabSyncManager.HeaderName, DependencyProvider.GetInstance<TabSyncManager>()),
			new TabItemViewModel(TabTabControl.HeaderName, DependencyProvider.GetInstance<TabTabControl>()),
			new TabItemViewModel(TabTextBox.HeaderName, DependencyProvider.GetInstance<TabTextBox>()),
			new TabItemViewModel(TabTextEditor.HeaderName, DependencyProvider.GetInstance<TabTextEditor>()),
			new TabItemViewModel(TabThemes.HeaderName, DependencyProvider.GetInstance<TabThemes>()),
			new TabItemViewModel(TabThemeVariantScope.HeaderName, DependencyProvider.GetInstance<TabThemeVariantScope>()),
			new TabItemViewModel(TabToggleButton.HeaderName, DependencyProvider.GetInstance<TabToggleButton>()),
			new TabItemViewModel(TabToggleSwitch.HeaderName, DependencyProvider.GetInstance<TabToggleSwitch>()),
			new TabItemViewModel(TabToolTip.HeaderName, DependencyProvider.GetInstance<TabToolTip>()),
			new TabItemViewModel(TabTreeView.HeaderName, DependencyProvider.GetInstance<TabTreeView>()),
			new TabItemViewModel(TabUpdateable.HeaderName, DependencyProvider.GetInstance<TabUpdateable>()),
			new TabItemViewModel(TabWeakEvents.HeaderName, DependencyProvider.GetInstance<TabWeakEvents>())
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
	public IDependencyProvider DependencyProvider { get; }

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