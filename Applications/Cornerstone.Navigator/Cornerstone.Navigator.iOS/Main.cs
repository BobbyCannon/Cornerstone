#region References

using Cornerstone.Runtime;
using UIKit;

#endregion

namespace Cornerstone.Navigator.iOS;

public class Application
{
	#region Methods

	private static void Main(string[] args)
	{
		AppBootstrap.Initialize("Cornerstone.Navigator", typeof(Application).Assembly, args);
		UIApplication.Main(args, null, typeof(AppDelegate));
	}

	#endregion
}