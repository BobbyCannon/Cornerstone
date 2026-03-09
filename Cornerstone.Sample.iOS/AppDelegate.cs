#region References

using Avalonia;
using Avalonia.iOS;
using Foundation;

#endregion

namespace Cornerstone.Sample.iOS;

/// <summary>
/// The UIApplicationDelegate for the application. This class is responsible for launching the
/// User Interface of the application, as well as listening (and optionally responding) to
/// application events from iOS.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : AvaloniaAppDelegate<App>
{
	#region Methods

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		return base.CustomizeAppBuilder(builder);
	}

	#endregion
}