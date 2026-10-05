#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Theme;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Navigation;

#endregion

namespace Cornerstone.Sample;

public partial class AppView : CornerstoneAppView<AppViewModel>
{
	#region Constructors

	public AppView() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	public AppView(AppViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void OnLoaded(RoutedEventArgs e)
	{
		if (ViewModel.State.RuntimeInformation.DevicePlatform
			is DevicePlatform.Android
			or DevicePlatform.IOS)
		{
			Menu.AutoExpandOnResize = false;
			Menu.DisplayMode = SplitViewDisplayMode.Overlay;
		}

		base.OnLoaded(e);
	}

	#endregion
}