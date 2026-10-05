#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.DesignTime;

#endregion

namespace Cornerstone.MediaPlayer.Views;

public partial class MainWindow : Window<AppViewModel>
{
	#region Constructors

	public MainWindow() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	public MainWindow(AppViewModel viewModel) : base(viewModel)
	{
		if (!Design.IsDesignMode)
		{
			this.RestoreWindowLocation(ViewModel.Settings.WindowLocation);
		}

		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		ViewModel.Settings.WindowLocation ??= new WindowLocation();
		ViewModel.Settings.WindowLocation.UpdateWith(this.GetWindowLocation());
		base.OnClosing(e);
	}

	#endregion
}
