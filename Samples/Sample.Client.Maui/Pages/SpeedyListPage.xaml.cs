namespace Sample.Client.Maui.Pages;

public partial class SpeedyListPage
{
	#region Constructors

	public SpeedyListPage(MauiViewManager manager)
	{
		InitializeComponent();

		BindingContext = manager;
		ViewManager = manager;
	}

	#endregion

	#region Properties

	public MauiViewManager ViewManager { get; }

	#endregion
}