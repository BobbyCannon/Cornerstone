using Cornerstone.Runtime;

namespace CornerstoneApplication.ViewModels;

public class 
MainViewModel : ViewModelBase
{
    #region Constructors

    public MainViewModel() : this(AppBootstrap.GetInstance<IRuntimeInformation>())
    {
    }

    public MainViewModel(IRuntimeInformation runtimeInformation)
	{
		Greeting = "Welcome to Cornerstone!";
		RuntimeInformation = runtimeInformation;
	}

	#endregion

	#region Properties

	public string Greeting { get; set; }

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion
}
