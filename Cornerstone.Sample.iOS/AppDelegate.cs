#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.iOS;
using Cornerstone.Esri;
using Cornerstone.Vlc;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Sample;
using Foundation;

#endregion

namespace Cornerstone.Sample.iOS;

/// <summary>
/// The UIApplicationDelegate for the application. This class is responsible for launching the
/// User Interface of the application, as well as listening (and optionally responding) to
/// application events from iOS.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : CornerstoneAppDelegate<App>
{
	#region Methods

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		return base.CustomizeAppBuilder(builder)
			.With(new iOSPlatformOptions { NativeBehindComposition = SampleNativeAirspace.IsEnabled })
			.UseiOS()
			.UseCornerstone([])
			.UseCornerstoneEsri()
			.UseCornerstoneVlc();
	}

	#endregion
}