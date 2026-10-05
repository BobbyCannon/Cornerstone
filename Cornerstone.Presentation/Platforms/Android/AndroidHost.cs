#region References

using Android.App;
using Cornerstone.Platforms.Android;

#endregion

namespace Cornerstone.Presentation.Platforms.Android;

/// <summary>
/// Holds the current Android activity for native camera / media features that need a
/// lifecycle owner or window (CameraX, PixelCopy, system video intents).
/// Call <see cref="Initialize" /> from your main activity (e.g. OnCreate).
/// </summary>
public static class AndroidHost
{
	#region Properties

	public static Activity Activity { get; private set; }

	#endregion

	#region Methods

	public static void Initialize(Activity activity)
	{
		Activity = activity;
		AndroidPlatform.SetActivity(activity);
	}

	#endregion
}