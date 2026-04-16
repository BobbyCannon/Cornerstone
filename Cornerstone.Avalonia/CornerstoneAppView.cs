namespace Cornerstone.Avalonia;

public partial class CornerstoneAppView<T>
	: CornerstoneUserControl where T : class
{
	#region Constructors

	public CornerstoneAppView(T viewModel)
	{
		ViewModel = viewModel;
		DataContext = viewModel;
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial T ViewModel { get; set; }

	#endregion
}