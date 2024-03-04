namespace Sample.Client.Maui.Pages;

public partial class SettingsPage
{
	#region Constructors

	public SettingsPage(MauiViewManager manager)
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