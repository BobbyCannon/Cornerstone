#region References

using System.Linq;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Sample.ViewModels;

#endregion

namespace Cornerstone.Sample.Views;

public partial class MainView : CornerstoneMainView<MainViewModel>
{
	#region Constructors

	public MainView() : this(DesignModeDependencyProvider.Get<MainViewModel>(), null)
	{
	}

	public MainView(MainViewModel viewModel, IDispatcher dispatcher) : base(viewModel, dispatcher)
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		ViewModel.SelectedTab = ViewModel.Tabs.FirstOrDefault(x => x.Header == ViewModel.ApplicationSettings.SelectedTabName)
			?? ViewModel.Tabs.FirstOrDefault();

		base.OnInitialized();
	}

	#endregion
}