#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Text;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.DesignTime;

#endregion

namespace Cornerstone.Sample;

public partial class AppWindow : Window<AppViewModel>
{
	#region Constructors

	public AppWindow() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	public AppWindow(AppViewModel viewModel) : base(viewModel)
	{
		if (!Design.IsDesignMode)
		{
			using (AppBootstrap.StartupProfiler.Start("RestoreWindowLocation"))
			{
				this.RestoreWindowLocation(ViewModel.State.Settings.WindowLocation);
			}
		}

		using (AppBootstrap.StartupProfiler.Start("AppWindow.InitializeComponent"))
		{
			InitializeComponent();
		}
	}

	#endregion

	#region Methods

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		ViewModel.State.Settings.WindowLocation.UpdateWith(this.GetWindowLocation());
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