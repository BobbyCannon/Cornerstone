#region References

using Cornerstone.Runtime;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Wpf;

[TestClass]
public class WpfRuntimeInformationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Details()
	{
		var runtimeInformation = new RuntimeInformation();
		IsFalse(string.IsNullOrWhiteSpace(runtimeInformation.ApplicationDataLocation));
		IsFalse(string.IsNullOrWhiteSpace(runtimeInformation.ApplicationFileName));
		IsFalse(string.IsNullOrWhiteSpace(runtimeInformation.ApplicationFilePath));
		IsFalse(string.IsNullOrWhiteSpace(runtimeInformation.ApplicationLocation));
		IsFalse(string.IsNullOrWhiteSpace(runtimeInformation.DeviceId));
	}

	#endregion
}