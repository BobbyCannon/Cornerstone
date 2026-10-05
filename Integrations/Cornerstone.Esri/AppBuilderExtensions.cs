#region References

using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Esri;

public static class AppBuilderExtensions
{
	#region Methods

	/// <summary>
	/// Registers the platform Esri MapView adapter. Call from the host after UseCornerstone.
	/// License / API key is host startup (ArcGISRuntimeEnvironment), not this method.
	/// </summary>
	public static AppBuilder UseCornerstoneEsri(this AppBuilder appBuilder)
	{
		return appBuilder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = AppBootstrap.DependencyProvider;
			#if ANDROID
			dependencyProvider.SetTransient<IMapViewAdapter, Platforms.Android.MapViewAdapter>();
			dependencyProvider.SetTransient<ISceneViewAdapter, Platforms.Android.SceneViewAdapter>();
			#elif IOS
			dependencyProvider.SetTransient<IMapViewAdapter, Platforms.iOS.MapViewAdapter>();
			dependencyProvider.SetTransient<ISceneViewAdapter, Platforms.iOS.SceneViewAdapter>();
			#elif WINDOWS
			dependencyProvider.SetTransient<IMapViewAdapter, Platforms.Windows.MapViewAdapter>();
			dependencyProvider.SetTransient<ISceneViewAdapter, Platforms.Windows.SceneViewAdapter>();
			#else
			dependencyProvider.SetTransient<IMapViewAdapter, MapViewAdapterStub>();
			dependencyProvider.SetTransient<ISceneViewAdapter, SceneViewAdapterStub>();
			#endif
		});
	}

	#endregion
}
