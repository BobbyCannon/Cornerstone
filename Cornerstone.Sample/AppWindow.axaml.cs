#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Rendering;
using Cornerstone.Avalonia;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Sample;

public partial class AppWindow : CornerstoneWindow<AppViewModel>
{
	#region Constructors

	public AppWindow() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	public AppWindow(AppViewModel viewModel) : base(viewModel)
	{
		if (!Design.IsDesignMode)
		{
			RestoreWindowLocation(ViewModel.State.Settings.WindowLocation);
		}

		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		ViewModel.State.Settings.WindowLocation.UpdateWith(GetWindowLocation());
		base.OnClosing(e);
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		Title += $" ({(ViewModel.State.RuntimeInformation.ApplicationIsElevated ? "administrator, " : "")}";
		Title += $"{ViewModel.State.RuntimeInformation.ApplicationStartup.Humanize()})";
		//RendererDiagnostics.DebugOverlays = RendererDebugOverlays.Fps;
		base.OnLoaded(e);
	}

	#endregion
}