#region References

using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Sample.ViewModels;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Views;

public partial class MainView : CornerstoneUserControl
{
	#region Constructors

	public MainView() : this(ViewModelProviderForDesignMode.Get<MainViewModel>())
	{
	}

	public MainView(MainViewModel viewModel)
	{
		ViewModel = viewModel;
		DataContext = ViewModel;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public MainViewModel ViewModel { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		ViewModel.SelectedTab = ViewModel.Tabs.FirstOrDefault(x => x.Header == ViewModel.ApplicationSettings.SelectedTabName)
			?? ViewModel.Tabs.FirstOrDefault();
		base.OnInitialized();
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		CornerstoneRuntimeInformation.Instance.CompleteLoad();
		base.OnLoaded(e);
	}

	#endregion
}