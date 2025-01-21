#region References

using Cornerstone.Sample.Tabs;
using Cornerstone.Testing;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests.Samples;

[TestClass]
public class TabUpdateableTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void TestUpdates()
	{
		var actual = TabUpdateable.TestUpdates(10000);
		actual.Dump();
	}

	#endregion
}