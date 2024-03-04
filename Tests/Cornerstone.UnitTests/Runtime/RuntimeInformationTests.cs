#region References

using System;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Runtime;

[TestClass]
public class RuntimeInformationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetBaseRuntimeInformation()
	{
		var actual = new RuntimeInformation();

		#if (NET48)
		AreEqual(new Version(4, 0, 30319, 42000), actual.DotNetRuntimeVersion);
		#else
		AreEqual(new Version(8, 0, 2), actual.DotNetRuntimeVersion);
		#endif

		//GenerateAsserts(actual);
	}

	#endregion
}