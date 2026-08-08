#region References

using Avalonia;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Avalonia.Platforms;

public static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseCornerstone(this AppBuilder appBuilder, string[] args)
	{
		return UseCornerstone<object>(appBuilder, args, out var value);
	}

	public static AppBuilder UseCornerstone<T>(this AppBuilder appBuilder, string[] args, out T value) where T : class
	{
		if (AppBootstrap.IsInitialized && args is { Length: > 0 })
		{
			AppBootstrap.ApplicationArguments.Parse(args);
		}

		#if ANDROID
		value = null;
		Android.AppBuilderExtensions.UseCornerstone(appBuilder, args);
		#elif BROWSER
		Browser.AppBuilderExtensions.UseCornerstone(appBuilder, args, out value);
		#elif IOS
		value = null;
		iOS.AppBuilderExtensions.UseCornerstone(appBuilder, args);
		#elif WINDOWS
		value = null;
		Windows.AppBuilderExtensions.UseCornerstone(appBuilder, args);
		#endif

		value = null;
		return appBuilder;
	}

	#endregion
}