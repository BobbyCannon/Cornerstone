#region References

using Cornerstone.Presentation.Controls;
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
	}

	#endregion
}
