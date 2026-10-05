#region References

using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Vlc;

public static class AppBuilderExtensions
{
	#region Methods

	/// <summary>
	/// Registers the platform LibVLC VideoView adapter. Call from the host after UseCornerstone.
	/// LibVLC and LibVLCSharp are LGPL-2.1-or-later. This does not reference the Windows GPL plugin package.
	/// </summary>
	public static AppBuilder UseCornerstoneVlc(this AppBuilder appBuilder)
	{
		return appBuilder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = AppBootstrap.DependencyProvider;
			#if ANDROID
			dependencyProvider.SetTransient<IVideoViewAdapter, Platforms.Android.VideoViewAdapter>();
			#elif IOS
			dependencyProvider.SetTransient<IVideoViewAdapter, Platforms.iOS.VideoViewAdapter>();
			#elif WINDOWS
			dependencyProvider.SetTransient<IVideoViewAdapter, Platforms.Windows.VideoViewAdapter>();
			#else
			dependencyProvider.SetTransient<IVideoViewAdapter, VideoViewAdapterStub>();
			#endif
		});
	}

	#endregion
}
