#region References

using System;
using System.Runtime.CompilerServices;
using Cornerstone.Runtime;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Runtime;

public class RuntimeInformationTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Information()
	{
		var runtimeInformation = new RuntimeInformation();
		runtimeInformation.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationName), "UnitTest");
		runtimeInformation.Initialize(typeof(Babel).Assembly);
		runtimeInformation.Refresh();

		AreEqual(10, runtimeInformation.Count);
		AreEqual("""
				ApplicationDataLocation
				ApplicationIsDevelopmentBuild
				ApplicationIsElevated
				ApplicationIsNativeBuild
				ApplicationLocation
				ApplicationName
				ApplicationVersion
				DeviceName
				DevicePlatform
				DeviceType
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