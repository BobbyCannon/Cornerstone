#region References

using Avalonia.Sample.ViewModels;
using Cornerstone.Avalonia;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabRuntimeInformation : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Runtime Information";

	#endregion

	#region Constructors

	public TabRuntimeInformation() : this(DesignModeDependencyProvider.Get<MainViewModel>())
	{
	}

	public TabRuntimeInformation(MainViewModel mainViewModel)
	{
		MainViewModel = mainViewModel;
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public MainViewModel MainViewModel { get; }

	#endregion
}