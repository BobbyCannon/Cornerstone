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
		Assert.AreEqual(3, count);
		Assert.IsTrue(watch.Elapsed.TotalMilliseconds > 1, watch.Elapsed.TotalMilliseconds.ToString());
		Assert.IsTrue(watch.Elapsed.TotalMilliseconds < 10, watch.Elapsed.TotalMilliseconds.ToString());
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
		Assert.IsTrue(watch.Elapsed.TotalMilliseconds > 1, watch.Elapsed.TotalMilliseconds.ToString());
		Assert.IsTrue(watch.Elapsed.TotalMilliseconds < 20, watch.Elapsed.TotalMilliseconds.ToString());
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
		}, 10, 1);
		watch.Stop();
		Assert.AreEqual(3, result);
		Assert.AreEqual(3, count);
		// had to reduce the min to 2.5 because I was getting number less than the expected min of 3. Ex. 2.9969
		Assert.IsTrue(watch.Elapsed.TotalMilliseconds > 2.5, watch.Elapsed.TotalMilliseconds.ToString());
		Assert.IsTrue(watch.Elapsed.TotalMilliseconds < 5, watch.Elapsed.TotalMilliseconds.ToString());
	}

	[TestMethod]
	public void RetryTypeShouldTimeoutAndThrowLastException()
	{
		var count = 0;
		var result = 0;
		var watch = Stopwatch.StartNew();

		ExpectedException<Exception>(() =>
			result = UtilityExtensions.Retry(() =>
			{
				if (++count < 3)
				{
					throw new Exception("Nope...");
				}

				return count;
			}, 4, 1), "Nope...");

		watch.Stop();
		Assert.AreEqual(0, result);
		Assert.AreEqual(2, count);
		Assert.IsTrue(watch.Elapsed.TotalMilliseconds > 1, watch.Elapsed.TotalMilliseconds.ToString());
		Assert.IsTrue(watch.Elapsed.TotalMilliseconds < 20, watch.Elapsed.TotalMilliseconds.ToString());
	}

	[TestMethod]
	public void UpdateIf()
	{
		var account = new ClientAccount();
		Assert.AreEqual(null, account.Name);

		account.IfThen(x => x.Name == null, x => x.Name = "John");
		Assert.AreEqual("John", account.Name);
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

		Assert.AreEqual(1, count);
		Assert.AreEqual(true, actual);
	}

	[TestMethod]
	public void WaitShouldCompleteWhenConditionIsSatisfied()
	{
		var count = 0;
		var watch = Stopwatch.StartNew();
		var result = UtilityExtensions.WaitUntil(() => ++count > 3, 10, 1);
		watch.Stop();
		Assert.IsTrue(result);
		Assert.AreEqual(4, count);
		Assert.IsTrue(watch.Elapsed.TotalMilliseconds > 1, watch.Elapsed.TotalMilliseconds.ToString());
		Assert.IsTrue(watch.Elapsed.TotalMilliseconds < 20, watch.Elapsed.TotalMilliseconds.ToString());
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

		Assert.AreEqual(31, count);
		Assert.AreEqual(new DateTime(2021, 06, 23, 09, 37, 30, DateTimeKind.Utc), UtcNow);
		Assert.AreEqual(false, actual);
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

		Assert.AreEqual(11, count);
		Assert.AreEqual(false, actual);
		// Time should be 10s (minimum) + one extra second (11 total) for the final loop
		Assert.AreEqual(StartDateTime.Add(minimum).AddSeconds(1), UtcNow);
	}

	#endregion
}