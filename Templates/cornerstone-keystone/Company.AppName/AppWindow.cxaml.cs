#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Theme;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName;

public partial class AppWindow : Window<AppViewModel>
{
	#region Constructors

	public AppWindow() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	public AppWindow(AppViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
		HomeView.DataContext = viewModel.Home;
	}

	#endregion
}
