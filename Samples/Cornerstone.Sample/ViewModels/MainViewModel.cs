#region References

using System;
using Cornerstone.Attributes;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Collections;
using Cornerstone.Location;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Sample.Tabs;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Sample.ViewModels;

public class MainViewModel : ViewModel
{
	#region Constructors

	[DependencyInjectionConstructor]
	public MainViewModel(
		ApplicationSettings applicationSettings,
		BrowserInteropProxy browserInteropProxy,
		IDependencyProvider dependencyProvider,
		ILocationProvider locationProvider,
		IDateTimeProvider timeProvider,
		IRuntimeInformation runtimeInformation,
		IDispatcher dispatcher)
		: base(dispatcher)
	{
		ApplicationSettings = applicationSettings;
		BrowserInteropProxy = browserInteropProxy;
		DependencyProvider = dependencyProvider;
		LocationProvider = locationProvider;
		TimeProvider = timeProvider;
		RuntimeInformation = runtimeInformation;

		Tabs = [];

		AddTabItemViewModel(TabThemes.HeaderName, "Icons.Color", typeof(TabThemes));
		AddTabItemViewModel(TabButton.HeaderName, "Icons.Button", typeof(TabButton));
		AddTabItemViewModel(TabCircularProgress.HeaderName, "Icons.Info", typeof(TabCircularProgress));
		AddTabItemViewModel(TabDebounceAndThrottle.HeaderName, "Icons.Signal", typeof(TabDebounceAndThrottle), browser: false);
		AddTabItemViewModel(TabDockingManager.HeaderName, "Icons.Window.Restore", typeof(TabDockingManager), browser: false);
		AddTabItemViewModel(TabMenu.HeaderName, "Icons.Menu", typeof(TabMenu));
		AddTabItemViewModel(TabNavigationMenu.HeaderName, "Icons.Grid", typeof(TabNavigationMenu));
		AddTabItemViewModel(TabNotificationCard.HeaderName, "Icons.Copy", typeof(TabNotificationCard));
		AddTabItemViewModel(TabResponsiveGrid.HeaderName, "Icons.List", typeof(TabResponsiveGrid));
		AddTabItemViewModel(TabRuntimeInformation.HeaderName, "Icons.Info", typeof(TabRuntimeInformation));
		AddTabItemViewModel(TabSecurityKeys.HeaderName, "Icons.Tag", typeof(TabSecurityKeys), browser: false);
		AddTabItemViewModel(TabSpeedyList.HeaderName, "Icons.Stashes", typeof(TabSpeedyList));
		AddTabItemViewModel(TabSpeedyTree.HeaderName, "Icons.Tree", typeof(TabSpeedyTree));
		AddTabItemViewModel(TabToggleButton.HeaderName, "Icons.Toggle.Button", typeof(TabToggleButton));
		AddTabItemViewModel(TabToggleSwitch.HeaderName, "Icons.Toggle.Switches", typeof(TabToggleSwitch));
		AddTabItemViewModel(TabWeakEvents.HeaderName, "Icons.Blame", typeof(TabWeakEvents), browser: false);
		AddTabItemViewModel(TabWebView.HeaderName, "Icons.Globe", typeof(TabWebView), browser: false);
		AddTabItemViewModel(TabWrapPanel.HeaderName, "Icons.Window.Maximize", typeof(TabWrapPanel));

		#if DEBUG
		AddTabItemViewModel(TabDevelopment.HeaderName, "Icons.Error", typeof(TabDevelopment));
		#endif
	}

	#endregion

	#region Properties

	public ApplicationSettings ApplicationSettings { get; }

	public BrowserInteropProxy BrowserInteropProxy { get; }

	public IDependencyProvider DependencyProvider { get; }

	public ILocationProvider LocationProvider { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	public TabItemReferenceViewModel SelectedTab { get; set; }

	public SpeedyList<TabItemReferenceViewModel> Tabs { get; }

	public IDateTimeProvider TimeProvider { get; }

	#endregion

	#region Methods

	public override void Initialize()
	{
		ApplicationSettings.Load();

		SelectedTab = Tabs.FirstOrDefault(x => x.TabName == ApplicationSettings.SelectedTabName)
			?? Tabs.FirstOrDefault(x => x.TabName == TabThemes.HeaderName)
			?? Tabs.FirstOrDefault();

		base.Initialize();
	}

	public override void Uninitialize()
	{
		ApplicationSettings.SelectedTabName = SelectedTab?.TabName ?? TabThemes.HeaderName;
		ApplicationSettings.Save();
		base.Uninitialize();
	}

	private void AddTabItemViewModel(string name, string icon, Type type, bool browser = true)
	{
		if ((RuntimeInformation.DevicePlatform == DevicePlatform.Browser) && !browser)
		{
			return;
		}

		Tabs.Add(new TabItemReferenceViewModel(name, icon, type));
	}

	#endregion
}