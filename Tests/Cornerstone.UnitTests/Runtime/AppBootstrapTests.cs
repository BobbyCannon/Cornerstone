#region References

using System;
using Cornerstone.Platforms;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Runtime;

[TestClass]
public class AppBootstrapTests : CornerstoneUnitTest
{
	#region Methods

	[TestCleanup]
	public void Cleanup()
	{
		AppBootstrap.Reset();
	}

	[TestMethod]
	public void InitializeCreatesCoreServicesAndPlatform()
	{
		AppBootstrap.Initialize("AppBootstrapTests", typeof(Babel).Assembly);

		IsTrue(AppBootstrap.IsInitialized);
		IsNotNull(AppBootstrap.ApplicationArguments);
		IsNotNull(AppBootstrap.DependencyProvider);
		IsNotNull(AppBootstrap.RuntimeInformation);
		// Dev builds may append ".Development" to the cached ApplicationName on Windows.
		IsTrue(AppBootstrap.RuntimeInformation.ApplicationName.StartsWith("AppBootstrapTests"));

		IsTrue(AppBootstrap.TryGetPlatform(out var platform));
		IsNotNull(platform);
		IsTrue(platform.IsLifecycleInitialized());
		IsTrue(platform.IsLifecycleLoaded());

		IsNotNull(AppBootstrap.GetInstance<IRuntimeInformation>());
		AreEqual(AppBootstrap.RuntimeInformation, AppBootstrap.GetInstance<RuntimeInformation>());
	}

	[TestMethod]
	public void InitializeIsIdempotentOnlyViaEnsure()
	{
		AppBootstrap.Initialize("AppBootstrapTests", typeof(Babel).Assembly);
		AppBootstrap.EnsureInitialized("OtherName", typeof(Babel).Assembly);
		IsTrue(AppBootstrap.RuntimeInformation.ApplicationName.StartsWith("AppBootstrapTests"));

		ExpectedException<CornerstoneException>(() =>
			AppBootstrap.Initialize("Again", typeof(Babel).Assembly)
		);
	}

	[TestMethod]
	public void GetInstanceBeforeInitializeThrows()
	{
		AppBootstrap.Reset();
		ExpectedException<CornerstoneException>(() => AppBootstrap.GetInstance<IRuntimeInformation>());
	}

	[TestMethod]
	public void InfrastructureLifecycleStartAndShutdown()
	{
		AppBootstrap.Initialize("AppBootstrapTests", typeof(Babel).Assembly);
		AppBootstrap.InitializeInfrastructure();
		AppBootstrap.StartInfrastructure();

		IsTrue(AppBootstrap.RuntimeInformation.IsLifecycleStarted());
		IsTrue(AppBootstrap.TryGetPlatform(out var platform));
		IsTrue(platform.IsLifecycleStarted());

		AppBootstrap.ShutdownInfrastructure();
		IsFalse(AppBootstrap.RuntimeInformation.IsLifecycleStarted());
	}

	[TestMethod]
	public void ProfileStartupArgumentCreatesStartupProfiler()
	{
		IsNull(AppBootstrap.StartupProfiler);

		AppBootstrap.Initialize(
			"AppBootstrapTests",
			typeof(Babel).Assembly,
			args: ["-ProfileStartup"]
		);

		IsNotNull(AppBootstrap.StartupProfiler);
		IsFalse(AppBootstrap.StartupProfiler.IsCompleted);
		IsTrue(AppBootstrap.StartupProfiler.Samples.Count >= 1);
		AreEqual("AppBootstrap.Initialize", AppBootstrap.StartupProfiler.Samples[0].Name);
	}

	[TestMethod]
	public void ProfileStartupNotEnabledByDefault()
	{
		AppBootstrap.Initialize("AppBootstrapTests", typeof(Babel).Assembly);
		IsNull(AppBootstrap.StartupProfiler);
	}

	[TestMethod]
	public void ResetClearsStartupProfiler()
	{
		AppBootstrap.Initialize(
			"AppBootstrapTests",
			typeof(Babel).Assembly,
			args: ["-ProfileStartup"]
		);
		IsNotNull(AppBootstrap.StartupProfiler);

		AppBootstrap.Reset();
		IsNull(AppBootstrap.StartupProfiler);
		IsFalse(AppBootstrap.IsInitialized);
	}

	#endregion
}