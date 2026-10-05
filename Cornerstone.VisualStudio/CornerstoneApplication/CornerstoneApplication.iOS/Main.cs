#region References

using UIKit;

#endregion

namespace CornerstoneApplication.iOS;

public class Application
{
	#region Methods

	private static void Main(string[] args)
	{
		UIApplication.Main(args, null, typeof(AppDelegate));
	}

	#endregion
}
