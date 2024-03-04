namespace Sample.Client.Maui.Pages;

public partial class LogInPage
{
	#region Constructors

	public LogInPage(MauiViewManager manager)
	{
		InitializeComponent();

		ViewManager = manager;
		BindingContext = manager;
	}

	#endregion

	#region Properties

	public MauiViewManager ViewManager { get; }

	#endregion
}