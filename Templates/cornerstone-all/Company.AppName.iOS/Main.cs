#region References

using Cornerstone.Runtime;
using UIKit;

#endregion

namespace Company.AppName.iOS;

public class Application
{
	#region Methods

	private static void Main(string[] args)
	{
		AppBootstrap.Initialize("Company.AppName", typeof(Application).Assembly, args);
		UIApplication.Main(args, null, typeof(AppDelegate));
	}

	#endregion
}
