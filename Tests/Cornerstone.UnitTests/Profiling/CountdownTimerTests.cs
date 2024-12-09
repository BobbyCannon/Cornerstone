#region References

using System;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class CountdownTimerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Remaining()
	{
		var countdownTimer = new CountdownTimer(this, TimeSpan.FromMinutes(5), null);
		AreEqual(TimeSpan.FromMinutes(5), countdownTimer.Remaining);
		AreEqual(100m, countdownTimer.RemainingPercent);
		AreEqual("05:00", countdownTimer.RemainingLabel);

		IncrementTime(minutes: 1);

		AreEqual(TimeSpan.FromMinutes(4), countdownTimer.Remaining);
		AreEqual(80m, countdownTimer.RemainingPercent);
	}
	
	[TestMethod]
	public void RemainingLabel()
	{
		AreEqual("60:00", new CountdownTimer(this, TimeSpan.FromMinutes(60), null).RemainingLabel);
		AreEqual("99999:00", new CountdownTimer(this, TimeSpan.FromMinutes(99999), null).RemainingLabel);
	}

	#endregion
}