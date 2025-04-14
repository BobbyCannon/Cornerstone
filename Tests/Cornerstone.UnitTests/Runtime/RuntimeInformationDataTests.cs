#region References

using System.Linq;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Testing;
using Cornerstone.Text;
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
		var data = GetRuntimeInformation();
		var actual = data.Keys.ToArray();
		var expected = new[]
		{
			"ApplicationBitness",
			"ApplicationDataLocation",
			"ApplicationFileName",
			"ApplicationFilePath",
			"ApplicationIsDevelopmentBuild",
			"ApplicationIsElevated",
			"ApplicationIsLoaded",
			"ApplicationIsShuttingDown",
			"ApplicationLocation",
			"ApplicationName",
			"ApplicationVersion",
			"DeviceDisplaySize",
			"DeviceId",
			"DeviceManufacturer",
			"DeviceMemory",
			"DeviceModel",
			"DeviceName",
			"DevicePlatform",
			"DevicePlatformBitness",
			"DevicePlatformVersion",
			"DeviceType",
			"DotNetRuntimeVersion"
		};

		AreEqual(expected, actual, () => actual.DumpCSharp(new CodeWriterSettings { TextFormat = TextFormat.Indented }));
	}

	#endregion
}