#region References

using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Sample;

public partial class AppWindow : CornerstoneWindow<AppViewModel>
{
	#region Constructors

	public AppWindow() : this(CornerstoneApplication.GetInstance<AppViewModel>())
	{
	}

	public AppWindow(AppViewModel viewModel) : base(viewModel)
	{
		if (!Design.IsDesignMode)
		{
			RestoreWindowLocation(ViewModel.ApplicationSettings.MainWindowLocation);
		}

		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		(ViewModel.SelectedTab.Control?.DataContext as ViewModel)?.Uninitialize();

		CornerstoneApplication.RuntimeInformation.StartShutdown();
		CornerstoneDispatcher.Instance.IsEnabled = false;

		ViewModel.ApplicationSettings.MainWindowLocation ??= new WindowLocation();
		ViewModel.ApplicationSettings.MainWindowLocation.UpdateWith(GetWindowLocation());
		ViewModel.Uninitialize();

		base.OnClosing(e);
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		Title += $" ({(ViewModel.RuntimeInformation.ApplicationIsElevated ? "administrator, " : "")}";
		Title += $"{ViewModel.RuntimeInformation.ApplicationStartup.Humanize()})";
		base.OnLoaded(e);
	}

	#endregion
}