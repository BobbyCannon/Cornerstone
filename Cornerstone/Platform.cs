#region References

#if ANDROID
using Cornerstone.Platforms.Android;

#elif BROWSER
using Cornerstone.Platforms.Browser;

#elif IOS
using Cornerstone.Platforms.iOS;

#elif WINDOWS
using Cornerstone.Platforms.Windows;
#endif

#endregion

namespace Cornerstone;

public static class Platform
{
	#region Methods

	public static T GetInstance<T>()
	{
		#if ANDROID
		var dp = AndroidPlatform.DependencyProvider;
		#elif BROWSER
		var dp = BrowserPlatform.DependencyProvider;
		#elif IOS
		var dp = IOSPlatform.DependencyProvider;
		#elif WINDOWS
		var dp = WindowsPlatform.DependencyProvider;
		#else
		DependencyProvider dp = null;
		#endif

		return dp != null
			? dp.GetInstance<T>()
			: Activator.CreateInstance<T>();
	}

	#endregion
}