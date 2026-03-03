#region References

using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample;

public partial class AppView : CornerstoneUserControl<AppViewModel>
{
	#region Constructors

	public AppView() : this(GetInstance<AppViewModel>())
	{
	}

	public AppView(AppViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		CornerstoneApplication.RuntimeInformation.CompleteStartup();
	}

	#endregion

	#region Methods

	protected override void OnLoaded(RoutedEventArgs e)
	{
		if (ViewModel.RuntimeInformation.DevicePlatform
			is DevicePlatform.Android
			or DevicePlatform.IOS)
		{
			Menu.AutoExpandOnResize = false;
			Menu.DisplayMode = ViewModel.RuntimeInformation.DeviceType == DeviceType.Tablet
				? SplitViewDisplayMode.Inline
				: SplitViewDisplayMode.Overlay;
		}

		base.OnLoaded(e);
	}

	#endregion
}