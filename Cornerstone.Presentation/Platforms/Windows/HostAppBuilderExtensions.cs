#region References

using Cornerstone.Input;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.Camera;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.MediaPlayer;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Web;

#endregion

namespace Cornerstone.Presentation.Platforms.Windows;

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