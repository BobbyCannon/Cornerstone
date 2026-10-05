#region References

using Android.App;
using Android.Runtime;
using Cornerstone.MediaPlayer;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Android;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.MediaPlayer.Android;

[Application]
public class Application : CornerstoneAndroidApplication<App>
{
	#region Constructors

	protected Application(nint javaReference, JniHandleOwnership transfer)
		: base(javaReference, transfer)
	{
		AppBootstrap.Initialize("Cornerstone.MediaPlayer", typeof(Application).Assembly);
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
