#region References

using Android.App;
using Android.Runtime;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Android;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Runtime;

#endregion

namespace CornerstoneApplication.Android;

[Application]
public class Application : CornerstoneAndroidApplication<App>
{
	#region Constructors

	protected Application(nint javaReference, JniHandleOwnership transfer)
		: base(javaReference, transfer)
	{
		AppBootstrap.Initialize("CornerstoneApplication", typeof(Application).Assembly);
	}

	#endregion

	#region Methods

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		return base.CustomizeAppBuilder(builder)
			.UseAndroid()
			.UseCornerstone([]);
	}

	#endregion
}
