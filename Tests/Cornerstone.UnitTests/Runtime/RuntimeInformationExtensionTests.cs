#region References

using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Runtime;

[TestClass]
public class RuntimeInformationExtensionTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Copy()
	{
		var runtimeInformation = new RuntimeInformation();
		runtimeInformation.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationName), "UnitTest");
		runtimeInformation.Initialize(typeof(Babel).Assembly);
		runtimeInformation.Refresh();
		var actual = runtimeInformation.Copy();
		AreEqual(runtimeInformation.Keys, actual.Keys);
		AreEqual(runtimeInformation, actual);
		IsFalse(ReferenceEquals(runtimeInformation, actual));
	}

	#endregion
}