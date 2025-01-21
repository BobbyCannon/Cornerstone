#region References

#if ANDROID
using Cornerstone.Android;

#elif WINDOWS
using Cornerstone.Windows;
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
		#elif WINDOWS
		var dp = WindowsPlatform.DependencyProvider;
		#else
		DependencyProvider dp = null;
		#endif

		return dp != null ? dp.GetInstance<T>() : default;
	}

	#endregion
}