#region References

using Android.App;
using Cornerstone.Platforms.Android;

#endregion

namespace Cornerstone.Presentation.Platforms.Android;

/// <summary>
/// Holds the current Android activity for native features that need a window.
/// CornerstoneActivity registers itself. A host can still call Initialize from OnCreate.
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