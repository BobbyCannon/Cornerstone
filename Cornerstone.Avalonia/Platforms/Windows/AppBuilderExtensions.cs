#region References

using Avalonia;
using Cornerstone.Avalonia.Camera;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.MediaPlayer;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Avalonia.Platforms.Windows;

internal static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseCornerstone(AppBuilder builder, string[] args)
	{
		return builder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = AppBootstrap.DependencyProvider;
			dependencyProvider.SetTransient<IWebViewAdapter, WebView2Adapter>();
			dependencyProvider.SetTransient<ICameraAdapter, CameraAdapter>();
			dependencyProvider.SetTransient<BaseMediaPlayerAdapter, MediaPlayerAdapter>();
		});
	}

	#endregion
}