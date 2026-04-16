#region References

using System;
using System.Threading;
using Cornerstone.Extensions;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests.Extensions;

[TestClass]
public class UtilityTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void RetryHandlesConcurrentLikeScenariosWithoutThunderingHerd()
	{
		var callCount = 0;
		Utility.Retry(() =>
		{
			callCount++;
			if (callCount < 2)
			{
				throw new Exception("retry");
			}
		}, 10, 1);

		AreEqual(2, callCount);
	}

	[TestMethod]
	public void RetrySucceedsAfterFewAttempts()
	{
		var callCount = 0;
		Utility.Retry(() =>
		{
			callCount++;
			if (callCount < 3)
			{
				throw new InvalidOperationException("Not yet");
			}
		}, 50, 1);

		AreEqual(3, callCount);
	}

	[TestMethod]
	public void RetrySucceedsOnFirstAttempt()
	{
		var callCount = 0;
		Utility.Retry(() => { callCount++; }, 1000, 10);
		AreEqual(1, callCount);
	}

	[TestMethod]
	public void RetryThrowsAfterTimeout()
	{
		ExpectedException<AggregateException>(() => { Utility.Retry(() => throw new InvalidOperationException("Always fails"), 2, 1); });
	}

	[TestMethod]
	public void WaitUntilGenericVersionWorks()
	{
		var counter = 0;
		var result = 42
			.WaitUntil(x =>
			{
				counter++;
				return (x > 40) && (counter > 2);
			}, 30, 1);

		IsTrue(result);
		IsTrue(counter >= 3);
	}

	[TestMethod]
	public void WaitUntilHonorsCancellationToken()
	{
		using var cts = new CancellationTokenSource();
		cts.CancelAfter(10);

		var start = DateTime.UtcNow;

		ExpectedException<OperationCanceledException>(() => { Utility.WaitUntil(() => false, 50, 1, cancellationToken: cts.Token); });

		var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
		IsTrue(elapsed < 20, () => $"Cancellation took too long: {elapsed} ms");
	}

	[TestMethod]
	public void WaitUntilRespectsMaximum()
	{
		// Use very short timeout + max
		var result = Utility.WaitUntil(() => false, 30, 10, 0, 50);
		IsFalse(result);
	}

	[TestMethod]
	public void WaitUntilRespectsMinimumWait()
	{
		var start = DateTime.UtcNow;
		var result = Utility.WaitUntil(() => true, 20, 1, 10, 50);
		IsTrue(result);
		var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
		IsTrue(elapsed >= 10, () => $"Minimum wait not respected: {elapsed} ms");
	}

	[TestMethod]
	public void WaitUntilReturnsFalseAfterTimeout()
	{
		var start = DateTime.UtcNow;
		var result = Utility.WaitUntil(() => false, 20, 1, maximum: 50);
		IsFalse(result);
		var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
		IsTrue(elapsed is >= 20 and < 50, () => $"Elapsed was {elapsed} ms");
	}

	[TestMethod]
	public void WaitUntilReturnsTrueImmediatelyOnFastPath()
	{
		var result = Utility.WaitUntil(() => true, 1000, 50);
		IsTrue(result);
	}

	#endregion
}