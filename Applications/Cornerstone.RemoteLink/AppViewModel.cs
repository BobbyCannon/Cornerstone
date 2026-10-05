#region References

using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Theme.Theming;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.RemoteLink.AirPlay;
using Cornerstone.RemoteLink.Android;
using Cornerstone.RemoteLink.Keystone;
using Cornerstone.RemoteLink.Keystone.State;
using Cornerstone.RemoteLink.Vnc;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.RemoteLink;

[SourceReflection]
[Notifiable(["*"])]
[DependencyInjected]
[DependencyInjected(typeof(IAppDispatcher))]
[DependencyInjected(typeof(IAppNavigator))]
public partial class AppViewModel : ApplicationViewModel
{
	#region Fields

	private bool _persistSelectedTab;
	private bool _shellStarted;
	private readonly AppState _state;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AppViewModel(
		AppBus bus,
		AppState state,
		AirPlayMirrorService service,
		AndroidMirrorService androidMirrorService,
		VncConnectionService vncConnectionService,
		ClipboardService clipboardService,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		IRuntimeInformation runtimeInformation
	) : base(dependencyProvider, dispatcher, 120)
	{
		Bus = bus;
		_state = state;
		RuntimeInformation = runtimeInformation;
		AirPlayTab = Track(new AirPlayTabViewModel(service, dispatcher));
		AndroidTab = Track(new AndroidTabViewModel(androidMirrorService, Settings, clipboardService, dispatcher));
		VncTab = Track(new VncTabViewModel(vncConnectionService, clipboardService, Settings));
		ShellTabs = new PresentationList<IShellTab>(dispatcher);
		ShellTabs.Add(AirPlayTab);
		ShellTabs.Add(AndroidTab);
		ShellTabs.Add(VncTab);
		SelectedShellTab = AirPlayTab;
		dependencyProvider.ExpectSingleton(this);
	}

	#endregion

	#region Properties

	public AirPlayTabViewModel AirPlayTab { get; }

	public AndroidTabViewModel AndroidTab { get; }

	public AppBus Bus { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	[Notify]
	public partial IShellTab SelectedShellTab { get; set; }

	public AppSettings Settings => _state.Settings;

	public PresentationList<IShellTab> ShellTabs { get; }

	public VncTabViewModel VncTab { get; }

	#endregion

	#region Methods

	public override void StartLifecycle()
	{
		_state.Settings.ApplyTheme();
		RestoreSelectedTab();
		_persistSelectedTab = true;
		base.StartLifecycle();
		_shellStarted = true;
		RefreshAndroidIfSelected();
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		if (propertyName == nameof(SelectedShellTab))
		{
			if (_persistSelectedTab && (SelectedShellTab != null))
			{
				Settings.SelectedShellTab = SelectedShellTab.DisplayName;
			}

			if (_shellStarted)
			{
				RefreshAndroidIfSelected();
			}
		}

		base.OnPropertyChanged(propertyName, oldValue, newValue);
	}

	private void RefreshAndroidIfSelected()
	{
		if (SelectedShellTab == AndroidTab)
		{
			_ = AndroidTab.RefreshOnFirstSelectAsync();
		}
	}

	private void RestoreSelectedTab()
	{
		var name = Settings.SelectedShellTab;
		if (string.IsNullOrWhiteSpace(name))
		{
			return;
		}

		foreach (var tab in ShellTabs)
		{
			if (tab.DisplayName == name)
			{
				SelectedShellTab = tab;
				return;
			}
		}
	}

	[RelayCommand]
	public void ToggleThemeMode()
	{
		Settings.ThemeMode = Settings.ThemeMode == ThemeMode.Dark
			? ThemeMode.Light
			: ThemeMode.Dark;
	}

	#endregion
}
