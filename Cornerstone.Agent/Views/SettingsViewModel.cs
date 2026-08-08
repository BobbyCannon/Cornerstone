#region References

using Cornerstone.Agent.Keystone;
using Cornerstone.Agent.Keystone.State;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
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
	[UpdateableAction(UpdateableAction.All)]
	public partial bool RecurseModelDirectory { get; set; }

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

	[RelayCommand]
	private async void SelectModelDirectory()
	{
		var directory = await CornerstoneApplication.TrySelectFolderAsync();
		if (directory != null)
		{
			ModelDirectory = directory;
		}
	}

	#endregion
}