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
				ApplicationDataLocation
				ApplicationFileName
				ApplicationFilePath
				ApplicationIsDevelopmentBuild
				ApplicationIsElevated
				ApplicationIsNativeBuild
				ApplicationLocation
				ApplicationName
				ApplicationVersion
				AvaloniaRuntimeVersion
				DeviceName
				DevicePlatform
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
	}

	#endregion
}