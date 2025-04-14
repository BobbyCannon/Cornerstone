#region References

using Avalonia;
using Cornerstone.Avalonia.Camera;
using Cornerstone.Avalonia.MediaPlayer;
using Cornerstone.Avalonia.WebView;

#endregion

namespace Cornerstone.Avalonia.Platforms.Android;

public static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseCornerstone(this AppBuilder builder)
	{
		return builder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = CornerstoneApplication.DependencyProvider;
			dependencyProvider.AddOrUpdateTransient<ICameraAdapter, CameraAdapter>();
			dependencyProvider.AddSingleton<IMediaPlayerAdapter, MediaPlayerAdapter>();
			dependencyProvider.AddOrUpdateTransient<IWebViewAdapter, WebViewAdapter>();
		});
	}

	#endregion
}