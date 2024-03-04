#region References

using System;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class DetailedTimerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void PercentOf()
	{
		var parentTimer = new DetailedTimer("Parent", null, this, null);
		parentTimer.Start();
		AreEqual(TimeSpan.Zero, parentTimer.Elapsed);
		AreEqual(-1, parentTimer.Percent);
		IncrementTime(seconds: 1);
		AreEqual(TimeSpan.FromSeconds(1), parentTimer.Elapsed);
	}

	#endregion
}