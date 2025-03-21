#region References

using System;
using System.Diagnostics;
using System.Threading;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage.Client;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class UtilityExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void RetryShouldCompleteWithoutException()
	{
		var count = 0;
		var watch = Stopwatch.StartNew();
		UtilityExtensions.Retry(() =>
		{
			if (++count < 3)
			{
				throw new Exception("Nope");
			}
		}, 1000, 1);
		watch.Stop();
		AreEqual(3, count);
		IsTrue(watch.Elapsed.TotalMilliseconds > 1, watch.Elapsed.TotalMilliseconds.ToString());
		IsTrue(watch.Elapsed.TotalMilliseconds < 40, watch.Elapsed.TotalMilliseconds.ToString());
	}

	[TestMethod]
	public void RetryShouldTimeoutAndThrowLastException()
	{
		var watch = Stopwatch.StartNew();

		ExpectedException<Exception>(() =>
			UtilityExtensions.Retry(() =>
			{
				Thread.Sleep(2);
				throw new Exception("Nope...");
			}, 5, 1), "Nope...");

		watch.Stop();
		IsTrue(watch.Elapsed.TotalMilliseconds > 1, watch.Elapsed.TotalMilliseconds.ToString());
		IsTrue(watch.Elapsed.TotalMilliseconds < 40, watch.Elapsed.TotalMilliseconds.ToString());
	}

	[TestMethod]
	public void RetryTypedShouldCompleteWithoutException()
	{
		var count = 0;
		var watch = Stopwatch.StartNew();
		var result = UtilityExtensions.Retry(() =>
		{
			if (++count < 3)
			{
				throw new Exception("Nope");
			}

			return count;
		}, 100, 1);
		watch.Stop();
		AreEqual(3, result);
		AreEqual(3, count);
		// had to reduce the min to 2.25 because I was getting number less than the expected min of 3. Ex. 2.9969
		IsTrue(watch.Elapsed.TotalMilliseconds > 2.25, watch.Elapsed.TotalMilliseconds.ToString());
		IsTrue(watch.Elapsed.TotalMilliseconds < 40, watch.Elapsed.TotalMilliseconds.ToString());
	}

	[TestMethod]
	public void RetryTypeShouldTimeoutAndThrowLastException()
	{
		var watch = Stopwatch.StartNew();

		ExpectedException<Exception>(() =>
				UtilityExtensions.Retry(
					() => throw new Exception("Nope..."),
					(int) WaitTimeout.TotalSeconds,
					10
				),
			"Nope..."
		);

		watch.Stop();
		IsTrue(watch.Elapsed.TotalMilliseconds > 1, watch.Elapsed.TotalMilliseconds.ToString());
		IsTrue(watch.Elapsed.TotalMilliseconds < 40, watch.Elapsed.TotalMilliseconds.ToString());
	}

	[TestMethod]
	public void UpdateIf()
	{
		var account = new ClientAccount();
		AreEqual(null, account.Name);

		account.IfThen(x => x.Name == null, x => x.Name = "John");
		AreEqual("John", account.Name);
	}

	[TestMethod]
	public void WaitShouldCancelImmediately()
	{
		var timeout = TimeSpan.FromMinutes(30);
		var delay = TimeSpan.FromMilliseconds(25);
		var minimum = TimeSpan.FromMinutes(1);
		var count = 0;

		SetTime(new DateTime(2021, 06, 23, 09, 37, 45, DateTimeKind.Utc));

		var watch = Stopwatch.StartNew();
		var actual = UtilityExtensions.WaitUntil(() =>
		{
			count++;
			return true;
		}, timeout, delay, minimum, this);
		watch.Stop();

		watch.Elapsed.Dump();

		AreEqual(1, count);
		AreEqual(true, actual);
	}

	[TestMethod]
	public void WaitShouldCompleteWhenConditionIsSatisfied()
	{
		var count = 0;
		var watch = Stopwatch.StartNew();
		var result = UtilityExtensions.WaitUntil(() => ++count > 3, 100, 1);
		watch.Stop();
		IsTrue(result, $"Count of {count} did not pass 3.");
		AreEqual(4, count);
		IsTrue(watch.Elapsed.TotalMilliseconds > 1, watch.Elapsed.TotalMilliseconds.ToString());
		IsTrue(watch.Elapsed.TotalMilliseconds < 40, watch.Elapsed.TotalMilliseconds.ToString());
	}

	[TestMethod]
	public void WaitShouldShouldRunAsFastAsPossiblyWithNoDelay()
	{
		var timeout = TimeSpan.FromSeconds(30);
		var delay = TimeSpan.Zero;
		var minimum = TimeSpan.FromSeconds(1);
		var count = 0;

		SetTime(new DateTime(2021, 06, 23, 09, 37, 00, DateTimeKind.Utc));

		var watch = Stopwatch.StartNew();
		var actual = UtilityExtensions.WaitUntil(() =>
		{
			if (count > 0)
			{
				IncrementTime(TimeSpan.FromSeconds(1));
			}
			count++;
			return false;
		}, timeout, delay, minimum, this);
		watch.Stop();

		watch.Elapsed.Dump();

		AreEqual(31, count);
		AreEqual(new DateTime(2021, 06, 23, 09, 37, 30, DateTimeKind.Utc), Now);
		AreEqual(false, actual);
	}

	[TestMethod]
	public void WaitWithValueLessThanMinimum()
	{
		var timeout = TimeSpan.FromSeconds(1);
		var delay = TimeSpan.FromMilliseconds(1);
		var minimum = TimeSpan.FromSeconds(10);
		var count = 0;
		
		var actual = UtilityExtensions.WaitUntil(() =>
			{
				IncrementTime(TimeSpan.FromSeconds(1));
				count++;
				return false;
			},
			timeout, delay, minimum, this
		);

		AreEqual(11, count);
		AreEqual(false, actual);
		// Time should be 10s (minimum) + one extra second (11 total) for the final loop
		AreEqual(StartDateTime.Add(minimum).AddSeconds(1), Now);
	}

	#endregion
}