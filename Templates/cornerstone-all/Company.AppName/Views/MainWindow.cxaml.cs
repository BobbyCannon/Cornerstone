#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName.Views;

public partial class MainWindow : Window<AppViewModel>
{
	#region Constructors

	public MainWindow() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	public MainWindow(AppViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	#endregion
}
