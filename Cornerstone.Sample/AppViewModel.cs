#region References

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone;
using Cornerstone.Sample.Tabs;

#endregion

namespace Cornerstone.Sample;

[SourceReflection]
[DependencyInjected]
[DependencyInjected(typeof(IAppDispatcher))]
[DependencyInjected(typeof(IAppNavigator))]
public partial class AppViewModel : ApplicationViewModel
{
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
		Tabs = [];

		NavigationMenuIsOpen = true;
		NavigationMenuDisplayMode = SplitViewDisplayMode.Inline;

		AddTabItemViewModel(TabWelcome.HeaderName, "Icons.Smile", typeof(TabWelcome));
		AddTabItemViewModel(TabThemes.HeaderName, "Icons.Color.Palette", typeof(TabThemes));
		AddTabItemViewModel(TabDocumentation.HeaderName, "Icons.Bookmark", typeof(TabDocumentation));
		AddTabItemViewModel(TabButton.HeaderName, "Icons.TapButton", typeof(TabButton));
		AddTabItemViewModel(TabCamera.HeaderName, "Icons.Camera", typeof(TabCamera), DevicePlatform.Windows | DevicePlatform.Android | DevicePlatform.IOS);
		AddTabItemViewModel(TabChannels.HeaderName, "Icons.Share.Fill", typeof(TabChannels));
		AddTabItemViewModel(TabDebounceAndThrottle.HeaderName, "Icons.Signal", new Thickness(0, 3, 0, -3), typeof(TabDebounceAndThrottle));
		AddTabItemViewModel(TabAppDispatcher.HeaderName, "Icons.DoubleArrow.Right", typeof(TabAppDispatcher));
		AddTabItemViewModel(TabDiagnostics.HeaderName, "Icons.Chart.Bar", typeof(TabDiagnostics));
		AddTabItemViewModel(TabDockingManager.HeaderName, "Icons.Folder", typeof(TabDockingManager), DevicePlatform.Windows);
		AddTabItemViewModel(TabGrids.HeaderName, "Icons.Grid", typeof(TabGrids));
		AddTabItemViewModel(TabInkCanvas.HeaderName, "Icons.Pencil.Square", typeof(TabInkCanvas));
		AddTabItemViewModel(TabMarkdownView.HeaderName, "Icons.Markdown", typeof(TabMarkdownView));
		AddTabItemViewModel(TabMediaPlayer.HeaderName, "Icons.Play", typeof(TabMediaPlayer), DevicePlatform.Windows | DevicePlatform.Android | DevicePlatform.IOS);
		AddTabItemViewModel(TabProgress.HeaderName, "Icons.Progress", new Thickness(0, 6, 0, -6), typeof(TabProgress));
		AddTabItemViewModel(TabProfiling.HeaderName, "Icons.Chart.Bar", new Thickness(0, 2, 0, -2), typeof(TabProfiling));
		AddTabItemViewModel(TabRuntimeInformation.HeaderName, "Icons.Info.Circle", typeof(TabRuntimeInformation));
		AddTabItemViewModel(TabShortcutBox.HeaderName, "Icons.Keyboard", typeof(TabShortcutBox));
		AddTabItemViewModel(TabSpeedyPack.HeaderName, "Icons.BoxLayered", typeof(TabSpeedyPack));
		AddTabItemViewModel(TabTokenTextFilter.HeaderName, "Icons.Search", typeof(TabTokenTextFilter));
		AddTabItemViewModel(TabTerminal.HeaderName, "Icons.Terminal", typeof(TabTerminal));
		AddTabItemViewModel(TabTextEditor.HeaderName, "Icons.File.Binary", typeof(TabTextEditor));
		AddTabItemViewModel(TabTreeDataGrid.HeaderName, "Icons.File.Tree", typeof(TabTreeDataGrid));
		AddTabItemViewModel(TabWebView.HeaderName, "Icons.Web", typeof(TabWebView));
	}

	#endregion

	#region Properties

	public AppBus Bus { get; }

	[Notify]
	public partial SplitViewDisplayMode NavigationMenuDisplayMode { get; set; }

	[Notify]
	public partial bool NavigationMenuIsOpen { get; set; }

	[Notify]
	public partial TabItemReferenceViewModel SelectedTab { get; set; }

	public AppState State { get; }

	public ObservableCollection<TabItemReferenceViewModel> Tabs { get; }

	#endregion

	#region Methods

	public override void StartLifecycle()
	{
		base.StartLifecycle();
		State.Settings.ApplyTheme();
		SelectedTab = Tabs.FirstOrDefault(x => x.TabTypeName == State.Settings.SelectedTab) ?? Tabs.FirstOrDefault();
	}

	/// <summary>
	/// Toggle Dark ↔ Light and persist (nav chrome; keeps AppSettings.ThemeMode in sync).
	/// </summary>
	[RelayCommand]
	public void ToggleThemeMode()
	{
		State.Settings.ThemeMode = State.Settings.ThemeMode == ThemeMode.Dark
			? ThemeMode.Light
			: ThemeMode.Dark;
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		if ((propertyName == nameof(SelectedTab)) && IsLifecycleStarted())
		{
			State.Settings.SelectedTab = SelectedTab.TabTypeName;
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

	#endregion
}