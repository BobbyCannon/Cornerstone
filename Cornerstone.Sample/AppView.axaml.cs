#region References

using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample;

public partial class AppView : CornerstoneAppView<AppViewModel>
{
	#region Constructors

	public AppView() : this(GetInstance<AppViewModel>())
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