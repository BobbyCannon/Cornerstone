#region References

using Cornerstone.Presentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
public class AdaptiveDispatchModeTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AppliedEntersActiveAndResetsStreak()
	{
		var isActive = false;
		var idleStreak = 3;

		AdaptiveDispatchMode.Advance(ref isActive, ref idleStreak, applied: true, requested: false, idleTicksBeforeThrottle: 8);

		IsTrue(isActive);
		AreEqual(0, idleStreak);
	}

	[TestMethod]
	public void EmptyTicksWhileIdleDoNotActivate()
	{
		var isActive = false;
		var idleStreak = 0;

		for (var i = 0; i < 20; i++)
		{
			AdaptiveDispatchMode.Advance(ref isActive, ref idleStreak, applied: false, requested: false, idleTicksBeforeThrottle: 8);
		}

		IsFalse(isActive);
	}

	[TestMethod]
	public void RequestedEntersActiveWithoutApply()
	{
		var isActive = false;
		var idleStreak = 0;

		AdaptiveDispatchMode.Advance(ref isActive, ref idleStreak, applied: false, requested: true, idleTicksBeforeThrottle: 8);

		IsTrue(isActive);
		AreEqual(0, idleStreak);
	}

	[TestMethod]
	public void StaysActiveUntilThresholdEmptyTicks()
	{
		var isActive = true;
		var idleStreak = 0;
		const int threshold = 3;

		AdaptiveDispatchMode.Advance(ref isActive, ref idleStreak, applied: false, requested: false, idleTicksBeforeThrottle: threshold);
		IsTrue(isActive);
		AreEqual(1, idleStreak);

		AdaptiveDispatchMode.Advance(ref isActive, ref idleStreak, applied: false, requested: false, idleTicksBeforeThrottle: threshold);
		IsTrue(isActive);
		AreEqual(2, idleStreak);

		AdaptiveDispatchMode.Advance(ref isActive, ref idleStreak, applied: false, requested: false, idleTicksBeforeThrottle: threshold);
		IsFalse(isActive);
		AreEqual(0, idleStreak);
	}

	[TestMethod]
	public void ApplyDuringStreakKeepsActive()
	{
		var isActive = true;
		var idleStreak = 5;

		AdaptiveDispatchMode.Advance(ref isActive, ref idleStreak, applied: true, requested: false, idleTicksBeforeThrottle: 8);

		IsTrue(isActive);
		AreEqual(0, idleStreak);
	}

	#endregion
}
