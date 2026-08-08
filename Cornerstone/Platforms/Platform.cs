#region References

using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms;

/// <summary>
/// TFM platform factory and bootstrap. The instance lives in the <see cref="DependencyProvider" /> as <see cref="IPlatform" />.
/// </summary>
public static class Platform
{
	#region Methods

	/// <summary>
	/// Create and register the TFM <see cref="IPlatform" /> (once), then run Initialize + Load.
	/// </summary>
	public static IPlatform Initialize(
		DependencyProvider dependencyProvider,
		RuntimeInformation runtimeInformation)
	{
		if (dependencyProvider.TryGetInstance<IPlatform>(out var existing))
		{
			EnsureInitializedAndLoaded(existing);
			return existing;
		}

		var platform = Create(dependencyProvider, runtimeInformation);
		dependencyProvider.AddSingleton(platform);
		EnsureInitializedAndLoaded(platform);
		return platform;
	}

	private static IPlatform Create(
		DependencyProvider dependencyProvider,
		RuntimeInformation runtimeInformation)
	{
		#if ANDROID
		return new Android.AndroidPlatform(dependencyProvider, runtimeInformation);
		#elif BROWSER
		return new Browser.BrowserPlatform(dependencyProvider, runtimeInformation);
		#elif IOS
		return new iOS.IOSPlatform(dependencyProvider, runtimeInformation);
		#elif WINDOWS
		return new Windows.WindowsPlatform(dependencyProvider, runtimeInformation);
		#else
		return new NullPlatform(dependencyProvider, runtimeInformation);
		#endif
	}

	private static void EnsureInitializedAndLoaded(IPlatform platform)
	{
		if (!platform.IsLifecycleInitialized())
		{
			platform.InitializeLifecycle();
		}

		if (!platform.IsLifecycleLoaded())
		{
			platform.LoadLifecycle();
		}
	}

	#endregion
}
