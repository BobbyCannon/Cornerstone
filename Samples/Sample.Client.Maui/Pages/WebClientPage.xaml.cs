namespace Sample.Client.Maui.Pages;

public partial class WebClientPage
{
	#region Constructors

	public WebClientPage(MauiViewManager manager)
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