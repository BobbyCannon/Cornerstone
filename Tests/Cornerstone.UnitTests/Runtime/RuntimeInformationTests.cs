#region References

using System;
using System.Runtime.CompilerServices;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Runtime;

[TestClass]
public class RuntimeInformationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Information()
	{
		var runtimeInformation = new RuntimeInformation();
		runtimeInformation.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationName), "UnitTest");
		runtimeInformation.Initialize(typeof(Babel).Assembly);
		runtimeInformation.Refresh();

		AreEqual("""
				ApplicationBitness
				ApplicationDataLocation
				ApplicationFileName
				ApplicationFilePath
				ApplicationIsDevelopmentBuild
				ApplicationIsElevated
				ApplicationIsLoaded
				ApplicationIsNativeBuild
				ApplicationIsShuttingDown
				ApplicationLocation
				ApplicationName
				ApplicationVersion
				AvaloniaRuntimeVersion
				DeviceDisplayRefreshRate
				DeviceDisplaySize
				DeviceId
				DeviceManufacturer
				DeviceMemory
				DeviceModel
				DeviceName
				DevicePlatform
				DevicePlatformBitness
				DevicePlatformVersion
				DeviceType
				DotNetRuntimeVersion
				""",
			string.Join(Environment.NewLine, runtimeInformation.Keys));

		IsTrue(runtimeInformation.ApplicationDataLocation.Length > 0);
		AreEqual(Environment.IsPrivilegedProcess, runtimeInformation.ApplicationIsElevated);
		AreEqual(!RuntimeFeature.IsDynamicCodeSupported, runtimeInformation.ApplicationIsNativeBuild);
		AreEqual(DevicePlatform.Windows, runtimeInformation.DevicePlatform);

		#if DEBUG
		IsTrue(runtimeInformation.ApplicationIsDevelopmentBuild);
		AreEqual("UnitTest.Development", runtimeInformation.ApplicationName);
		#else
		IsFalse(runtimeInformation.ApplicationIsDevelopmentBuild);
		AreEqual("UnitTest", runtimeInformation.ApplicationName);
		#endif

		IsTrue(runtimeInformation.IsLifecycleInitialized());
		IsTrue(runtimeInformation.IsLifecycleLoaded());
		IsFalse(runtimeInformation.IsLifecycleStarted());
	}

	[TestMethod]
	public void LifecyclePhases()
	{
		var runtimeInformation = new RuntimeInformation();
		runtimeInformation.SetApplicationAssembly(typeof(Babel).Assembly);

		IsFalse(runtimeInformation.IsLifecycleInitialized());
		IsFalse(runtimeInformation.IsLifecycleLoaded());
		IsFalse(runtimeInformation.ApplicationIsLoaded);

		runtimeInformation.InitializeLifecycle();
		IsTrue(runtimeInformation.IsLifecycleInitialized());
		IsFalse(runtimeInformation.IsLifecycleLoaded());
		IsFalse(runtimeInformation.ApplicationIsLoaded);

		runtimeInformation.LoadLifecycle();
		IsTrue(runtimeInformation.IsLifecycleLoaded());
		IsTrue(runtimeInformation.ApplicationIsLoaded);
		IsFalse(runtimeInformation.IsLifecycleStarted());
		AreEqual(TimeSpan.Zero, runtimeInformation.ApplicationStartup);

		runtimeInformation.StartLifecycle();
		IsTrue(runtimeInformation.IsLifecycleStarted());
		IsTrue(runtimeInformation.ApplicationStartup > TimeSpan.Zero);

		// Idempotent Start does not rewrite startup time
		var startup = runtimeInformation.ApplicationStartup;
		runtimeInformation.StartLifecycle();
		AreEqual(startup, runtimeInformation.ApplicationStartup);

		runtimeInformation.StopLifecycle();
		IsFalse(runtimeInformation.IsLifecycleStarted());
		IsTrue(runtimeInformation.ApplicationIsShuttingDown);

		runtimeInformation.UnloadLifecycle();
		IsFalse(runtimeInformation.IsLifecycleLoaded());
		IsFalse(runtimeInformation.ApplicationIsLoaded);

		runtimeInformation.UninitializeLifecycle();
		IsFalse(runtimeInformation.IsLifecycleInitialized());
	}

	[TestMethod]
	public void InitializeIsIdempotentForLifecycle()
	{
		var runtimeInformation = new RuntimeInformation();
		runtimeInformation.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationName), "UnitTest");
		runtimeInformation.Initialize(typeof(Babel).Assembly);
		runtimeInformation.Initialize(typeof(Babel).Assembly);

		IsTrue(runtimeInformation.IsLifecycleInitialized());
		IsTrue(runtimeInformation.IsLifecycleLoaded());
		IsTrue(runtimeInformation.ApplicationIsLoaded);
	}

	#endregion
}