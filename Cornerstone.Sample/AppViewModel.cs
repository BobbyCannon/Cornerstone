#region References

using Avalonia;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Tabs;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Sample;

[SourceReflection]
public partial class AppViewModel : ViewModel
{
	#region Constructors

	public AppViewModel(
		ApplicationSettings applicationSettings,
		IRuntimeInformation runtimeInformation)
	{
		ApplicationSettings = applicationSettings;
		RuntimeInformation = runtimeInformation;
		Tabs = [];

		AddTabItemViewModel(TabWelcome.HeaderName, "Icons.Smile", typeof(TabWelcome));
		AddTabItemViewModel(TabButton.HeaderName, "Icons.TapButton", typeof(TabButton));
		AddTabItemViewModel(TabChannels.HeaderName, "Icons.Share.Fill", typeof(TabChannels));
		AddTabItemViewModel(TabDebounceAndThrottle.HeaderName, "Icons.Signal", new Thickness(0, 3, 0, -3), typeof(TabDebounceAndThrottle));
		AddTabItemViewModel(TabDockingManager.HeaderName, "Icons.Folder", typeof(TabDockingManager), DevicePlatform.Windows);
		AddTabItemViewModel(TabGrids.HeaderName, "Icons.Grid", typeof(TabGrids));
		AddTabItemViewModel(TabInkCanvas.HeaderName, "Icons.Pencil.Square", typeof(TabInkCanvas));
		AddTabItemViewModel(TabMarkdownView.HeaderName, "Icons.Markdown", typeof(TabMarkdownView));
		AddTabItemViewModel(TabProgress.HeaderName, "Icons.Progress", new Thickness(0, 6, 0, -6), typeof(TabProgress));
		AddTabItemViewModel(TabProfiling.HeaderName, "Icons.Chart.Bar", new Thickness(0, 2, 0, -2), typeof(TabProfiling));
		AddTabItemViewModel(TabRuntimeInformation.HeaderName, "Icons.Info.Circle", typeof(TabRuntimeInformation));
		AddTabItemViewModel(TabShortcutBox.HeaderName, "Icons.Keyboard", typeof(TabShortcutBox));
		AddTabItemViewModel(TabSpeedyPack.HeaderName, "Icons.BoxLayered", typeof(TabSpeedyPack));
		AddTabItemViewModel(TabTextEditor.HeaderName, "Icons.File.Binary", typeof(TabTextEditor));
		AddTabItemViewModel(TabTreeDataGrid.HeaderName, "Icons.File.Tree", typeof(TabTreeDataGrid));
	}

	#endregion

	#region Properties

	public ApplicationSettings ApplicationSettings { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	[Notify]
	public partial TabItemReferenceViewModel SelectedTab { get; set; }

	public ObservableCollection<TabItemReferenceViewModel> Tabs { get; }

	#endregion

	#region Methods

	public override void Initialize()
	{
		ApplicationSettings.Load();
		SelectedTab = Tabs.FirstOrDefault(x => x.TabTypeName == ApplicationSettings.SelectedTab) ?? Tabs.FirstOrDefault();
		base.Initialize();
	}

	public override void Uninitialize()
	{
		base.Uninitialize();
		ApplicationSettings.Save();
	}

	protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		if (propertyName == nameof(SelectedTab))
		{
			ApplicationSettings.SelectedTab = SelectedTab.TabTypeName;
		}

		base.OnPropertyChanged(propertyName);
	}

	private void AddTabItemViewModel(string name, string icon, Type type, DevicePlatform platforms = DevicePlatform.All, bool onlyDebug = false)
	{
		AddTabItemViewModel(name, icon, new Thickness(0), type, platforms, onlyDebug);
	}

	private void AddTabItemViewModel(string name, string icon, Thickness iconMargin, Type type, DevicePlatform platforms = DevicePlatform.All, bool onlyDebug = false)
	{
		if (!platforms.HasFlag(RuntimeInformation.DevicePlatform)
			|| (onlyDebug && !Debugger.IsAttached))
		{
			return;
		}

		Tabs.Add(new TabItemReferenceViewModel(name, 0, icon, iconMargin, type, true));
	}

	#endregion
}