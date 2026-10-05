#region References

using Company.AppName;
using Cornerstone.Presentation;
using Cornerstone.Presentation.iOS;
using Cornerstone.Presentation.Platforms;
using Foundation;

#endregion

namespace Company.AppName.iOS;

[Register("AppDelegate")]
public class AppDelegate : CornerstoneAppDelegate<App>
{
	#region Methods

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		return base.CustomizeAppBuilder(builder)
			.UseiOS()
			.UseCornerstone([]);
	}

	#endregion
}
