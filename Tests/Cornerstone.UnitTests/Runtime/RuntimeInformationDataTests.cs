#region References

using System.Linq;
using Cornerstone.Runtime;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Runtime;

[TestClass]
public class RuntimeInformationDataTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Enumeration()
	{
		var data = (RuntimeInformationData) GetRuntimeInformation();
		var actual = data.Keys.ToArray();
		var expected = new[]
		{
			"ApplicationBitness",
			"ApplicationDataLocation",
			"ApplicationFileName",
			"ApplicationFilePath",
			"ApplicationIsDevelopmentBuild",
			"ApplicationIsElevated",
			"ApplicationLocation",
			"DeviceDisplaySize",
			"DeviceManufacturer",
			"DeviceMemory",
			"DeviceModel",
			"DevicePlatformBitness",
			"DotNetRuntimeVersion",
			"IsLoaded",
			"IsShuttingDown"
		};

		AreEqual(expected, actual, () => actual.DumpCSharp());
	}

	#endregion
}