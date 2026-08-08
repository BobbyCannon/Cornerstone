#region References

using Cornerstone.Platforms;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Platforms;

[TestClass]
public class PlatformLifecycleTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void InitializeRegistersOnDependencyProvider()
	{
		var runtimeInformation = new RuntimeInformation();
		runtimeInformation.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationName), "UnitTest");
		runtimeInformation.Initialize(typeof(Babel).Assembly);

		var provider = new DependencyProvider("PlatformLifecycleTests");
		var platform = Platform.Initialize(provider, runtimeInformation);

		IsNotNull(platform);
		IsTrue(provider.TryGetInstance<IPlatform>(out var resolved));
		AreEqual(platform, resolved);
		IsTrue(platform.IsLifecycleInitialized());
		IsTrue(platform.IsLifecycleLoaded());
		IsFalse(platform.IsLifecycleStarted());

		// Second call is idempotent and returns the same registered instance
		var again = Platform.Initialize(provider, runtimeInformation);
		AreEqual(platform, again);
		AreEqual(platform, provider.GetInstance<IPlatform>());

		platform.StartLifecycle();
		IsTrue(platform.IsLifecycleStarted());

		platform.StopLifecycle();
		IsFalse(platform.IsLifecycleStarted());
	}

	#endregion
}
