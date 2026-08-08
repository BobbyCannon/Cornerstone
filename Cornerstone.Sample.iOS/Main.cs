#region References

using Cornerstone.Runtime;
using UIKit;

#endregion

namespace Cornerstone.Sample.iOS;

public class Application
{
	#region Methods

	/// <summary>
	/// This is the main entry point of the application.
	/// </summary>
	private static void Main(string[] args)
	{
		AppBootstrap.Initialize("Cornerstone.Sample", typeof(Application).Assembly, args);

		// if you want to use a different Application Delegate class from "AppDelegate"
		// you can specify it here.
		UIApplication.Main(args, null, typeof(AppDelegate));
	}

	#endregion
}