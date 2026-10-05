#region References

using Cornerstone.Agent.Keystone;
using Cornerstone.Agent.Keystone.State;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Theme.Theming;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Platform.Storage;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Views;

[SourceReflection]
[DependencyInjected]
public partial class SettingsViewModel
	: DispatchableViewModel<AppSettings>,
		IAppSettings,
		IUpdateable<IAppSettings>
{
	#region Fields

	public static readonly string AssemblyName;
	private readonly IAppDispatcher _appDispatcher;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public SettingsViewModel(
		AppBus bus, AppState state,
		IAppDispatcher appDispatcher,
		IAppNavigator navigator
	) : base(state.Settings)
	{
		_appDispatcher = appDispatcher;

		Bus = bus;
		State = state;
		Navigator = navigator;

		AllowedDirectories = [];
		AutoUpdateModel = true;
		ModelStateView = TrackDispatchChild(new DispatchableViewModel<ModelState>(State.ModelState));
		RecurseModelDirectory = true;
		WindowLocation = new WindowLocation();
	}

	static SettingsViewModel()
	{
		AssemblyName = typeof(SettingsViewModel).ToAssemblyName();
	}

	#endregion

	#region Properties

	[UpdateableAction(UpdateableAction.All)]
	public PresentationList<string> AllowedDirectories { get; }

	public AppBus Bus { get; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string ModelDirectory { get; set; }

	public DispatchableViewModel<ModelState> ModelStateView { get; set; }
	public IAppNavigator Navigator { get; }

	[Notify]
	public partial bool IsRefreshingModels { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool RecurseModelDirectory { get; set; }

	[Notify]
	public partial string RefreshStatusMessage { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string SelectedModel { get; set; }

	public AppState State { get; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial ThemeColor ThemeColor { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool UseDarkMode { get; set; }

	[UpdateableAction(UpdateableAction.All)]
	public WindowLocation WindowLocation { get; }

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		TrackProperties(State.ModelState)
			.MapOneWay(nameof(ModelState.IsRefreshingModels), nameof(IsRefreshingModels), (bool v) => v)
			.MapOneWay(nameof(ModelState.RefreshStatusMessage), nameof(RefreshStatusMessage), (string v) => v ?? string.Empty);
		base.InitializeLifecycle();
	}

	[RelayCommand]
	private async void SelectModelDirectory()
	{
		var directory = await FilePickerHelper.TrySelectFolderAsync();
		if (directory != null)
		{
			ModelDirectory = directory;
		}
	}

	#endregion
}