#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Navigation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone;
using Cornerstone.Sample.Tabs.Welcome;
using TabAppDispatcher = Cornerstone.Sample.Tabs.AppDispatcher.TabAppDispatcherDemos;
using TabControls = Cornerstone.Sample.Tabs.Controls.TabControls;
using TabData = Cornerstone.Sample.Tabs.Data.TabData;
using TabInputs = Cornerstone.Sample.Tabs.Inputs.TabInputs;
using TabDocumentation = Cornerstone.Sample.Tabs.Documentation.TabDocumentation;
using TabKeystone = Cornerstone.Sample.Tabs.Keystone.TabKeystone;
using TabMaps = Cornerstone.Sample.Tabs.Maps.TabMaps;
using TabMedia = Cornerstone.Sample.Tabs.Media.TabMedia;
using TabObserve = Cornerstone.Sample.Tabs.Observe.TabObserve;

#endregion

namespace Cornerstone.Sample;

[SourceReflection]
[DependencyInjected]
[DependencyInjected(typeof(IAppDispatcher))]
[DependencyInjected(typeof(IAppNavigator))]
public partial class AppViewModel : ApplicationViewModel
{
	#region Constants

	public const string AboutMenuName = "About";

	public const string HomeMenuName = "Home";

	public const string SettingsMenuName = "Settings";

	#endregion

	#region Fields

	private Type _pendingSection;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AppViewModel(
		AppState state, AppBus bus,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher
	) : base(dependencyProvider, dispatcher, 120)
	{
		State = state;
		Bus = bus;
		ThemeSettings = new ThemeSettingsViewModel(state.Settings);
		Tabs = [];

		NavigationMenuIsOpen = false;
		NavigationMenuDisplayMode = SplitViewDisplayMode.Overlay;
		NavigationMenuAutoExpandOnResize = false;
		NavigationMenuAutoCollapseOnSelectionChange = false;

		AddTabItemViewModel(HomeMenuName, "Icons.Home", typeof(TabWelcome));
		AddTabItemViewModel(SettingsMenuName, "Icons.Cog", typeof(Tabs.SettingsView));
		AddTabItemViewModel(AboutMenuName, "Icons.Info.Circle", typeof(Tabs.AboutView));

		SearchItems =
		[
			new SampleSearchItem("Welcome", typeof(TabWelcome)),
			new SampleSearchItem("Welcome > Themes", typeof(TabWelcome)),
			new SampleSearchItem("Keystone", typeof(TabKeystone)),
			new SampleSearchItem("AppDispatcher", typeof(TabAppDispatcher)),
			new SampleSearchItem("Controls", typeof(TabControls)),
			new SampleSearchItem("Controls > Buttons", typeof(TabControls)),
			new SampleSearchItem("Controls > Charts", typeof(TabControls)),
			new SampleSearchItem("Controls > Docking Manager", typeof(TabControls)),
			new SampleSearchItem("Controls > Grids", typeof(TabControls)),
			new SampleSearchItem("Controls > Ink Canvas", typeof(TabControls)),
			new SampleSearchItem("Controls > Markdown", typeof(TabControls)),
			new SampleSearchItem("Controls > Menus", typeof(TabControls)),
			new SampleSearchItem("Controls > Number Box", typeof(TabControls)),
			new SampleSearchItem("Controls > Progress", typeof(TabControls)),
			new SampleSearchItem("Controls > Shortcut", typeof(TabControls)),
			new SampleSearchItem("Controls > Shortcuts", typeof(TabControls)),
			new SampleSearchItem("Controls > Tabs", typeof(TabControls)),
			new SampleSearchItem("Controls > Terminal", typeof(TabControls)),
			new SampleSearchItem("Controls > Text Editor", typeof(TabControls)),
			new SampleSearchItem("Controls > Token Text Filter", typeof(TabControls)),
			new SampleSearchItem("Controls > Tree Data Grid", typeof(TabControls)),
			new SampleSearchItem("Inputs", typeof(TabInputs)),
			new SampleSearchItem("Inputs > Gamepad", typeof(TabInputs)),
			new SampleSearchItem("Inputs > Keyboard and Mouse", typeof(TabInputs)),
			new SampleSearchItem("Maps", typeof(TabMaps)),
			new SampleSearchItem("Maps > Mapsui", typeof(TabMaps)),
			new SampleSearchItem("Maps > Esri", typeof(TabMaps)),
			new SampleSearchItem("Media", typeof(TabMedia)),
			new SampleSearchItem("Media > Camera", typeof(TabMedia)),
			new SampleSearchItem("Media > Media Player", typeof(TabMedia)),
			new SampleSearchItem("Media > VLC", typeof(TabMedia)),
			new SampleSearchItem("Media > WebView", typeof(TabMedia)),
			new SampleSearchItem("Data > Debounce / Throttle", typeof(TabData)),
			new SampleSearchItem("Sync", typeof(TabData)),
			new SampleSearchItem("Sync > Loopback", typeof(TabData)),
			new SampleSearchItem("Sync > Security Card Reader", typeof(TabData)),
			new SampleSearchItem("Sync > Speedy Pack", typeof(TabData)),
			new SampleSearchItem("Observe", typeof(TabObserve)),
			new SampleSearchItem("Observe > Diagnostics", typeof(TabObserve)),
			new SampleSearchItem("Observe > Profiler", typeof(TabObserve)),
			new SampleSearchItem("Observe > Runtime Information", typeof(TabObserve)),
			new SampleSearchItem("Documentation", typeof(TabDocumentation))
		];
	}

	#endregion

	#region Properties

	public AppBus Bus { get; }

	[Notify]
	public partial bool NavigationMenuAutoCollapseOnSelectionChange { get; set; }

	[Notify]
	public partial bool NavigationMenuAutoExpandOnResize { get; set; }

	[Notify]
	public partial SplitViewDisplayMode NavigationMenuDisplayMode { get; set; }

	[Notify]
	public partial bool NavigationMenuIsOpen { get; set; }

	public IReadOnlyList<SampleSearchItem> SearchItems { get; }

	[Notify]
	public partial SampleSearchItem SelectedSearchItem { get; set; }

	[Notify]
	public partial TabItemReferenceViewModel SelectedTab { get; set; }

	public AppState State { get; }

	public ObservableCollection<TabItemReferenceViewModel> Tabs { get; }

	public ThemeSettingsViewModel ThemeSettings { get; }

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		ThemeSettings.InitializeLifecycle();
		ThemeSettings.Attach(this);
		base.InitializeLifecycle();
	}

	public override void StartLifecycle()
	{
		base.StartLifecycle();
		State.Settings.ApplyTheme();
		RestoreMenu();
	}

	public void SelectTabByType(Type tabType)
	{
		if (tabType is null)
		{
			return;
		}

		SelectedTab = Tabs.First(x => x.TabName == HomeMenuName);
		OpenSection(tabType);
	}

	public Type TakePendingSection()
	{
		var section = _pendingSection;
		_pendingSection = null;
		return section;
	}

	public override void UninitializeLifecycle()
	{
		ThemeSettings.Detach(this);
		ThemeSettings.UninitializeLifecycle();
		base.UninitializeLifecycle();
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		if ((propertyName == nameof(SelectedTab)) && IsLifecycleStarted() && (SelectedTab is not null))
		{
			State.Settings.SelectedTab = SelectedTab.TabName;
		}

		if ((propertyName == nameof(SelectedSearchItem)) && (SelectedSearchItem is not null))
		{
			SelectTabByType(SelectedSearchItem.HubType);
		}

		base.OnPropertyChanged(propertyName, oldValue, newValue);
	}

	private void AddTabItemViewModel(string name, string icon, Type type, DevicePlatform platforms = DevicePlatform.All, bool onlyDebug = false)
	{
		AddTabItemViewModel(name, icon, new Thickness(0), type, platforms, onlyDebug);
	}

	private void AddTabItemViewModel(string name, string icon, Thickness iconMargin, Type type, DevicePlatform platforms = DevicePlatform.All, bool onlyDebug = false)
	{
		if (!platforms.HasFlag(State.RuntimeInformation.DevicePlatform)
			|| (onlyDebug && !Debugger.IsAttached))
		{
			return;
		}

		Tabs.Add(new TabItemReferenceViewModel(name, 0, icon, iconMargin, type, true));
	}

	private void OpenSection(Type sectionType)
	{
		if ((sectionType is null) || (sectionType == typeof(TabWelcome)))
		{
			return;
		}

		var home = Tabs.First(x => x.TabName == HomeMenuName);
		if (home.Control is TabWelcome welcome)
		{
			welcome.ShowSection(sectionType);
			return;
		}

		_pendingSection = sectionType;
	}

	private void RestoreMenu()
	{
		var saved = State.Settings.SelectedTab;
		var menu = Tabs.FirstOrDefault(x => x.TabName == saved);
		if (menu is not null)
		{
			SelectedTab = menu;
			return;
		}

		SelectedTab = Tabs.First(x => x.TabName == HomeMenuName);
		OpenSection(TabWelcome.MatchSavedSection(saved));
	}

	#endregion
}
