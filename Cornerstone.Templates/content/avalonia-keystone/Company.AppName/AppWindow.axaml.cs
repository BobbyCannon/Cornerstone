#region References

using Avalonia.Controls;
using Cornerstone.Avalonia;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName;

public partial class AppWindow : CornerstoneWindow<AppViewModel>
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
