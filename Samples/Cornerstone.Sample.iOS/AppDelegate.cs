#region References

using Avalonia;
using Avalonia.iOS;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Platforms.iOS;
using Cornerstone.Platforms.iOS;
using Foundation;

#endregion

namespace Cornerstone.Sample.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
public class AppDelegate : AvaloniaAppDelegate<App>
{
	#region Methods

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		var dependencyProvider = CornerstoneApplication.DependencyProvider;

		IOSPlatform.Initialize(dependencyProvider);

		var response = base
			.CustomizeAppBuilder(builder)
			.WithInterFont()
			.UseCornerstone();

		return response;
	}

	#endregion
}