#region References

using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Sample.ViewModels;

#endregion

namespace Cornerstone.Sample.Views;

public partial class MainView : CornerstoneMainView<MainViewModel>
{
	#region Constructors

	public MainView() : this(ViewDependencyProvider.Get<MainViewModel>(), null)
	{
	}

	public MainView(MainViewModel viewModel, IDispatcher dispatcher) : base(viewModel, dispatcher)
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void OnLoaded(RoutedEventArgs e)
	{
		if (ViewModel.RuntimeInformation.IsMobile())
		{
			Menu.AutoExpandOnResize = false;
			Menu.DisplayMode = SplitViewDisplayMode.Overlay;
		}

		base.OnLoaded(e);
	}

	#endregion
}