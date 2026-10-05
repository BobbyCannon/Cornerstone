#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.iOS;
using Foundation;

#endregion

namespace CornerstoneApplication.iOS;

[Register("AppDelegate")]
public partial class AppDelegate : CornerstoneAppDelegate<App>
{
	#region Methods

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		return base.CustomizeAppBuilder(builder);
	}

	#endregion
}
