#region References

using System;
using Cornerstone.Attributes;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Collections;
using Cornerstone.Extensions;
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
		IBrowserInterop browserInteropProxy,
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

		var allPlatformExceptBrowser = DevicePlatform.All.ClearFlag(DevicePlatform.Browser);

		AddTabItemViewModel(TabThemes.HeaderName, "Icons.Color", typeof(TabThemes));
		AddTabItemViewModel(TabBrowserInterop.HeaderName, "Icons.Browser", typeof(TabBrowserInterop), DevicePlatform.Browser);
		AddTabItemViewModel(TabButton.HeaderName, "Icons.Button", typeof(TabButton));
		AddTabItemViewModel(TabCamera.HeaderName, "Icons.Camera", typeof(TabCamera), allPlatformExceptBrowser);
		AddTabItemViewModel(TabCircularProgress.HeaderName, "Icons.Info", typeof(TabCircularProgress));
		AddTabItemViewModel(TabColorPicker.HeaderName, "Icons.Color", typeof(TabColorPicker));
		AddTabItemViewModel(TabDebounceAndThrottle.HeaderName, "Icons.Signal", typeof(TabDebounceAndThrottle));
		AddTabItemViewModel(TabDockingManager.HeaderName, "Icons.Window.Restore", typeof(TabDockingManager), allPlatformExceptBrowser);
		AddTabItemViewModel(TabFonts.HeaderName, "Icons.Font", typeof(TabFonts));
		AddTabItemViewModel(TabMediaPlayer.HeaderName, "Icons.TriangleRight", typeof(TabMediaPlayer), allPlatformExceptBrowser);
		AddTabItemViewModel(TabMenu.HeaderName, "Icons.Menu", typeof(TabMenu));
		AddTabItemViewModel(TabNavigationMenu.HeaderName, "Icons.Grid", typeof(TabNavigationMenu));
		AddTabItemViewModel(TabNotificationCard.HeaderName, "Icons.Copy", typeof(TabNotificationCard));
		AddTabItemViewModel(TabPermissions.HeaderName, "Icons.Permissions", typeof(TabPermissions));
		AddTabItemViewModel(TabResponsiveGrid.HeaderName, "Icons.List", typeof(TabResponsiveGrid));
		AddTabItemViewModel(TabRuntimeInformation.HeaderName, "Icons.Info", typeof(TabRuntimeInformation));
		AddTabItemViewModel(TabSecurityKeys.HeaderName, "Icons.Tag", typeof(TabSecurityKeys), DevicePlatform.Android | DevicePlatform.Windows);
		AddTabItemViewModel(TabSpeedyList.HeaderName, "Icons.Stashes", typeof(TabSpeedyList));
		AddTabItemViewModel(TabSpeedyTree.HeaderName, "Icons.Tree", typeof(TabSpeedyTree));
		AddTabItemViewModel(TabToggleButton.HeaderName, "Icons.Toggle.Button", typeof(TabToggleButton));
		AddTabItemViewModel(TabToggleSwitch.HeaderName, "Icons.Toggle.Switches", typeof(TabToggleSwitch));
		AddTabItemViewModel(TabWeakEvents.HeaderName, "Icons.Blame", typeof(TabWeakEvents), allPlatformExceptBrowser);
		AddTabItemViewModel(TabWebView.HeaderName, "Icons.Globe", typeof(TabWebView));
		AddTabItemViewModel(TabWrapPanel.HeaderName, "Icons.Window.Maximize", typeof(TabWrapPanel));

		#if DEBUG
		AddTabItemViewModel(TabDevelopment.HeaderName, "Icons.Error", typeof(TabDevelopment));
		#endif
	}

	#endregion

	#region Properties

	public ApplicationSettings ApplicationSettings { get; }

	public IBrowserInterop BrowserInteropProxy { get; }

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

	private void AddTabItemViewModel(string name, string icon, Type type, DevicePlatform platforms = DevicePlatform.All)
	{
		if (!platforms.HasFlag(RuntimeInformation.DevicePlatform))
		{
			return;
		}

		Tabs.Add(new TabItemReferenceViewModel(name, icon, type));
	}

	#endregion
}