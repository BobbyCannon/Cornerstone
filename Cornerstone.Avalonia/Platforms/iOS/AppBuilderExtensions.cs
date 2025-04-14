#region References

using Avalonia;
using Cornerstone.Avalonia.Camera;
using Cornerstone.Avalonia.MediaPlayer;
using Cornerstone.Runtime;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Avalonia.Platforms.iOS;

public static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseCornerstone(this AppBuilder builder)
	{
		return builder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = CornerstoneApplication.DependencyProvider;
			dependencyProvider.AddOrUpdateTransient<ICameraAdapter, CameraAdapterStub>();
			dependencyProvider.AddOrUpdateTransient<IMediaPlayerAdapter, MediaPlayerAdapterStub>();
			//dependencyProvider.AddOrUpdateTransient<IWebViewAdapter, WebViewAdapter>();
		});
	}

	#endregion
}