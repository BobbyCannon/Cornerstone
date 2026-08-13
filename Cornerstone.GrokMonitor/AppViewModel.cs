#region References

using System;
using System.Linq;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Data;
using Cornerstone.GrokMonitor.GrokUsage;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.GrokMonitor.Settings;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor;

/// <summary>
/// Host ViewModel: owns one home dashboard tab per Grok home, a Settings tab, and AppDispatcher projection.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[DependencyInjected]
[DependencyInjected(typeof(IAppDispatcher))]
[DependencyInjected(typeof(IAppNavigator))]
public partial class AppViewModel : ApplicationViewModel
{
	#region Fields

	private readonly IDispatcher _dispatcher;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AppViewModel(
		AppBus bus,
		AppState state,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		IRuntimeInformation runtimeInformation
	) : base(dependencyProvider, dispatcher, 120)
	{
		Bus = bus;
		State = state;
		_dispatcher = dispatcher;
		RuntimeInformation = runtimeInformation;
		HomeTabs = new PresentationList<GrokUsageTabViewModel>(dispatcher);
		ShellTabs = new PresentationList<IShellTab>(dispatcher);
		SettingsTab = new SettingsTabViewModel(state, dispatcher);
		dependencyProvider.ExpectSingleton(this);
	}

	#endregion

	#region Properties

	public AppBus Bus { get; }

	/// <summary>
	/// True when at least one Grok home folder was found.
	/// </summary>
	[Notify]
	public partial bool HasHomeTabs { get; set; }

	/// <summary>
	/// One usage dashboard per discovered Grok home (~/.grok, ~/.grok-work, …).
	/// </summary>
	public PresentationList<GrokUsageTabViewModel> HomeTabs { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	/// <summary>
	/// Active home usage tab when selection is a home dashboard (not Settings).
	/// </summary>
	public GrokUsageTabViewModel SelectedHomeTab => SelectedShellTab as GrokUsageTabViewModel;

	/// <summary>
	/// Active shell tab (home usage or Settings).
	/// </summary>
	[Notify]
	public partial IShellTab SelectedShellTab { get; set; }

	/// <summary>
	/// Always-present settings page (theme color, mode, density).
	/// </summary>
	public SettingsTabViewModel SettingsTab { get; }

	/// <summary>
	/// Shell TabControl items: home dashboards then Settings last.
	/// </summary>
	public PresentationList<IShellTab> ShellTabs { get; }

	/// <summary>
	/// Tab headers when more than one shell tab is present (homes and/or Settings).
	/// </summary>
	[Notify]
	public partial bool ShowShellTabHeaders { get; set; }

	public AppState State { get; }

	#endregion

	#region Methods

	public override void LoadLifecycle()
	{
		base.LoadLifecycle();
		// Settings load via AppState.Track; re-apply theme once UI/theme is ready.
		State.Settings.ApplyTheme();
	}

	public override void StartLifecycle()
	{
		// Dispatcher worker first so Track + Attach can project after homes load.
		base.StartLifecycle();

		// Theme already applied in LoadLifecycle / FinalizeLoad; re-apply after UI is live.
		State.Settings.ApplyTheme();

		SettingsTab.InitializeLifecycle();
		SettingsTab.LoadLifecycle();
		SettingsTab.StartLifecycle();
		Track(SettingsTab);
		SettingsTab.Attach(this);

		Bus.GrokUsage.EnsureHomes();
		Bus.GrokUsage.RefreshAll();
		SyncHomeTabs();

		// Clear list pending so the first Update() does not re-sync unnecessarily.
		State.GrokUsage.Homes.ClearHasPending();
	}

	public override void StopLifecycle()
	{
		foreach (var tab in HomeTabs.ToArray())
		{
			tab.StopLifecycle();
		}

		SettingsTab.StopLifecycle();
		base.StopLifecycle();
	}

	/// <summary>
	/// Toggle Dark ↔ Light and persist (window chrome; keeps AppSettings.ThemeMode in sync).
	/// </summary>
	[RelayCommand]
	public void ToggleThemeMode()
	{
		State.Settings.ThemeMode = State.Settings.ThemeMode == ThemeMode.Dark
			? ThemeMode.Light
			: ThemeMode.Dark;
	}

	public override void UninitializeLifecycle()
	{
		foreach (var tab in HomeTabs.ToArray())
		{
			tab.Detach(this);
			Release(tab);
			tab.UninitializeLifecycle();
		}

		HomeTabs.Clear();

		SettingsTab.Detach(this);
		Release(SettingsTab);
		SettingsTab.UninitializeLifecycle();

		ShellTabs.Clear();
		SelectedShellTab = null;
		HasHomeTabs = false;
		ShowShellTabHeaders = false;
		base.UninitializeLifecycle();
	}

	public override void UnloadLifecycle()
	{
		foreach (var tab in HomeTabs.ToArray())
		{
			tab.UnloadLifecycle();
		}

		SettingsTab.UnloadLifecycle();
		base.UnloadLifecycle();
	}

	/// <summary>
	/// Tab strip selection must update domain <see cref="GrokUsageState.SelectedHomeId" />.
	/// View-clock scrub/replay reprojects that home; without this, the second tab's slider
	/// only updates after a manual Refresh.
	/// </summary>
	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		if ((propertyName == nameof(SelectedShellTab)) && newValue is GrokUsageTabViewModel tab && (tab.HomeId != Guid.Empty))
		{
			if (State.GrokUsage.SelectedHomeId != tab.HomeId)
			{
				Bus.GrokUsage.SelectHome(tab.HomeId);
			}
		}

		base.OnPropertyChanged(propertyName, oldValue, newValue);
	}

	/// <summary>
	/// When refresh re-discovers homes, <see cref="State.GrokUsage.Homes" /> membership changes
	/// outside any tab projection — sync shell tabs on the dispatch tick.
	/// </summary>
	protected override bool Update()
	{
		var homesPending = State.GrokUsage.Homes.HasPending;
		if (homesPending)
		{
			State.GrokUsage.Homes.ClearHasPending();
			_dispatcher.Dispatch(SyncHomeTabs, DispatcherPriority.Render);
		}

		return base.Update() || homesPending;
	}

	/// <summary>
	/// Shell strip = home dashboards in discovery order, then Settings last.
	/// </summary>
	private void RebuildShellTabs()
	{
		ShellTabs.Clear();
		foreach (var tab in HomeTabs)
		{
			ShellTabs.Add(tab);
		}

		ShellTabs.Add(SettingsTab);
	}

	/// <summary>
	/// Adds tabs for new homes and tears down tabs whose homes left state.
	/// Preserves selection when the selected home still exists. Settings stays last.
	/// </summary>
	private void SyncHomeTabs()
	{
		var homes = State.GrokUsage.Homes;
		var selectedHomeId = SelectedHomeTab?.HomeId ?? State.GrokUsage.SelectedHomeId;
		var settingsWasSelected = ReferenceEquals(SelectedShellTab, SettingsTab);

		// Remove tabs for homes no longer in state.
		foreach (var tab in HomeTabs.ToArray())
		{
			if (homes.Any(h => h.Id == tab.HomeId))
			{
				continue;
			}

			if (ReferenceEquals(SelectedShellTab, tab))
			{
				SelectedShellTab = null;
			}

			tab.Detach(this);
			Release(tab);
			tab.UninitializeLifecycle();
			HomeTabs.Remove(tab);
		}

		// Add tabs for newly discovered homes (stable order: state list order).
		foreach (var home in homes)
		{
			if (HomeTabs.Any(t => t.HomeId == home.Id))
			{
				continue;
			}

			var tab = new GrokUsageTabViewModel(Bus, State, _dispatcher, home.Id);
			tab.InitializeLifecycle();
			tab.LoadLifecycle();
			tab.StartLifecycle();
			Track(tab);

			// Keep every home dashboard attached so inactive tabs stay projected.
			tab.Attach(this);
			HomeTabs.Add(tab);
		}

		// Keep tab order aligned with Homes (primary grok first after discovery sort).
		for (var i = 0; i < homes.Count; i++)
		{
			var homeId = homes[i].Id;
			var tabIndex = -1;
			for (var t = 0; t < HomeTabs.Count; t++)
			{
				if (HomeTabs[t].HomeId == homeId)
				{
					tabIndex = t;
					break;
				}
			}

			if ((tabIndex >= 0) && (tabIndex != i) && (i < HomeTabs.Count))
			{
				var tab = HomeTabs[tabIndex];
				HomeTabs.RemoveAt(tabIndex);
				HomeTabs.Insert(Math.Min(i, HomeTabs.Count), tab);
			}
		}

		RebuildShellTabs();

		HasHomeTabs = HomeTabs.Count > 0;
		ShowShellTabHeaders = ShellTabs.Count > 1;

		if (settingsWasSelected)
		{
			SelectedShellTab = SettingsTab;
		}
		else
		{
			var preferred = HomeTabs.FirstOrDefault(t => t.HomeId == selectedHomeId) ?? HomeTabs.FirstOrDefault();
			SelectedShellTab = preferred ?? (IShellTab) SettingsTab;
			if (SelectedHomeTab != null)
			{
				Bus.GrokUsage.SelectHome(SelectedHomeTab.HomeId);
			}
		}
	}

	#endregion
}