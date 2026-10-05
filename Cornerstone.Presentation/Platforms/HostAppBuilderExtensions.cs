#region References

using System;
using Cornerstone.Presentation.Controls.Camera;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.MediaPlayer;
using Cornerstone.Presentation.Controls.Web;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Presentation.Platforms;

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
		#else

		// net10.0: macOS WKWebView; Linux WebKit2GTK; other hosts stubs.
		value = null;
		appBuilder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = AppBootstrap.DependencyProvider;
			dependencyProvider.SetTransient<ICameraAdapter, CameraAdapterStub>(() =>
				new CameraAdapterStub());
			dependencyProvider.SetTransient<BaseMediaPlayerAdapter, MediaPlayerAdapterStub>(() =>
				new MediaPlayerAdapterStub());
			if (OperatingSystem.IsMacOS())
				dependencyProvider.SetTransient<IWebViewAdapter, Desktop.WebViewAdapter>();
			else if (OperatingSystem.IsLinux())
				dependencyProvider.SetTransient<IWebViewAdapter, Desktop.LinuxWebViewAdapter>();
			else
				dependencyProvider.SetTransient<IWebViewAdapter, WebViewAdapterStub>();
		});
		#endif

		return appBuilder;
	}

	#endregion
}