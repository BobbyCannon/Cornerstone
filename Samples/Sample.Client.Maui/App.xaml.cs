namespace Sample.Client.Maui;

public partial class App
{
	#region Constructors

	public App(MauiViewManager manager)
	{
		InitializeComponent();

		manager.Initialize();

		MainPage = new AppShell(manager);
	}

	#endregion
}