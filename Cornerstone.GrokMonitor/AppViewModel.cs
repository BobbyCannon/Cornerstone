#region References

using System;
using System.Linq;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Data;
using Cornerstone.GrokMonitor.GrokUsage;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.GrokMonitor.Keystone.State;
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

	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly IDispatcher _dispatcher;
	private readonly HomeTabProjection _homeTabProjection;
	private readonly AppState _state;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AppViewModel(
		AppBus bus,
		AppState state,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		IDateTimeProvider dateTimeProvider,
		IRuntimeInformation runtimeInformation
	) : base(dependencyProvider, dispatcher, 120)
	{
		Bus = bus;
		_state = state;
		_dateTimeProvider = dateTimeProvider ?? DateTimeProvider.RealTime;
		_dispatcher = dispatcher;
		RuntimeInformation = runtimeInformation;
		HomeTabs = new PresentationList<GrokUsageTabViewModel>(dispatcher);
		ShellTabs = new PresentationList<IShellTab>(dispatcher);
		SettingsTab = Track(new SettingsTabViewModel(state.Settings));
		_homeTabProjection = Track(new HomeTabProjection(this));
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
	/// Persisted settings (window location, theme). Host chrome may read this; Views bind SettingsTab.
	/// </summary>
	public AppSettings Settings => _state.Settings;

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

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		base.InitializeLifecycle();
		_homeTabProjection.Attach(this);
	}

	public override void StartLifecycle()
	{
		base.StartLifecycle();
		_state.Settings.ApplyTheme();
		Bus.GrokUsage.RefreshAll();
		ApplyHomeTabProjection();
	}

	/// <summary>
	/// Toggle Dark ↔ Light and persist (window chrome; keeps AppSettings.ThemeMode in sync).
	/// </summary>
	[RelayCommand]
	public void ToggleThemeMode()
	{
		SettingsTab.ThemeMode = SettingsTab.ThemeMode == ThemeMode.Dark
			? ThemeMode.Light
			: ThemeMode.Dark;
	}

	public override void UninitializeLifecycle()
	{
		_homeTabProjection.Detach(this);
		HomeTabs.Clear();
		ShellTabs.Clear();
		SelectedShellTab = null;
		HasHomeTabs = false;
		ShowShellTabHeaders = false;
		base.UninitializeLifecycle();
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
			if (_state.GrokUsage.SelectedHomeId != tab.HomeId)
			{
				Bus.GrokUsage.SelectHome(tab.HomeId);
			}
		}

		base.OnPropertyChanged(propertyName, oldValue, newValue);
	}

	/// <summary>
	/// Seeds or refreshes home tabs from State. Called after RefreshAll on start;
	/// later membership changes apply through AppDispatcher on this projection.
	/// </summary>
	internal void ApplyHomeTabProjection()
	{
		_homeTabProjection.ApplyModelChanges();
	}

	private GrokUsageTabViewModel CreateHomeTab(GrokHomeUsageState home)
	{
		var tab = new GrokUsageTabViewModel(
			Bus,
			home,
			_state.GrokUsage,
			_state.Settings,
			_dispatcher,
			_dateTimeProvider);
		Track(tab);
		return tab;
	}

	/// <summary>
	/// Shell strip = home dashboards in discovery order, then Settings last.
	/// Preserves selection when the selected home still exists.
	/// </summary>
	private void ProjectShellTabs()
	{
		var selectedHomeId = SelectedHomeTab?.HomeId ?? _state.GrokUsage.SelectedHomeId;
		var settingsWasSelected = ReferenceEquals(SelectedShellTab, SettingsTab);

		if ((SelectedHomeTab != null) && !HomeTabs.Contains(SelectedHomeTab))
		{
			SelectedShellTab = null;
		}

		ShellTabs.Clear();
		foreach (var tab in HomeTabs)
		{
			ShellTabs.Add(tab);
		}

		ShellTabs.Add(SettingsTab);

		HasHomeTabs = HomeTabs.Count > 0;
		ShowShellTabHeaders = ShellTabs.Count > 1;

		if (settingsWasSelected)
		{
			SelectedShellTab = SettingsTab;
			return;
		}

		var preferred = HomeTabs.FirstOrDefault(t => t.HomeId == selectedHomeId) ?? HomeTabs.FirstOrDefault();
		SelectedShellTab = preferred ?? (IShellTab) SettingsTab;
		if (SelectedHomeTab != null)
		{
			Bus.GrokUsage.SelectHome(SelectedHomeTab.HomeId);
		}
	}

	private void ReleaseHomeTab(GrokUsageTabViewModel tab)
	{
		if (ReferenceEquals(SelectedShellTab, tab))
		{
			SelectedShellTab = null;
		}

		Release(tab);
	}

	#endregion

	#region Classes

	/// <summary>
	/// AppViewModel is the dispatcher, not a DispatchableViewModel.
	/// This child is Attach'd to the host (IAppDispatcher) so TrackCollection
	/// can project Homes → HomeTabs on the apply loop.
	/// </summary>
	private sealed class HomeTabProjection : DispatchableViewModel
	{
		#region Constructors

		public HomeTabProjection(AppViewModel host)
		{
			TrackCollection(
				host._state.GrokUsage.Homes,
				host.HomeTabs,
				(home, tab) => home.Id == tab.HomeId,
				host.CreateHomeTab,
				(_, _) => { },
				host.ReleaseHomeTab);
			TrackDerived(host.ProjectShellTabs);
		}

		#endregion
	}

	#endregion
}
