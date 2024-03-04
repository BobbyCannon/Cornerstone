#region References

using System;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class TimeSpanExtensionsTests : CornerstoneUnitTest
{
	[TestMethod]
	public void PercentOf()
	{
		AreEqual(10.0m, TimeSpan.FromSeconds(1).PercentOf(TimeSpan.FromSeconds(10)));
		AreEqual(1.0m, TimeSpan.FromSeconds(1).PercentOf(TimeSpan.FromSeconds(100)));
		AreEqual(22.22m, TimeSpan.FromSeconds(2).PercentOf(TimeSpan.FromSeconds(9)));
	}
}