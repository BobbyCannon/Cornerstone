namespace CornerstoneApplication.ViewModels;

public class MainViewModel : ViewModelBase
{
	#region Constructors

	public MainViewModel()
	{
		Greeting = "Welcome to Cornerstone!";
	}

	#endregion

	#region Properties

	public string Greeting { get; set; }

	#endregion
}
