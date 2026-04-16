#region References

using System;
using System.Diagnostics;
using System.Threading;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// General utility extensions with low-allocation retry and wait logic.
/// </summary>
public static class Utility
{
	#region Methods

	/// <summary>
	/// Retries the action until it succeeds or the timeout is reached.
	/// Uses high-resolution timestamp for zero allocation timing.
	/// </summary>
	/// <param name="action"> The action to retry. </param>
	/// <param name="timeout"> Timeout in milliseconds (default 1000). </param>
	/// <param name="delay"> Delay between retries in milliseconds (default 10). </param>
	public static void Retry(Action action, int timeout = 1000, int delay = 10)
	{
		if (action is null)
		{
			throw new ArgumentNullException(nameof(action));
		}
		if (timeout < 0)
		{
			timeout = 0;
		}
		if (delay < 1)
		{
			delay = 1;
		}

		var startTimestamp = Stopwatch.GetTimestamp();
		var timeoutTicks = timeout * (Stopwatch.Frequency / 1000L);

		while (true)
		{
			Exception lastException = null;
			try
			{
				action();
				return;
			}
			catch (Exception ex)
			{
				lastException = ex;
			}

			var elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
			if (elapsedTicks >= timeoutTicks)
			{
				throw new AggregateException(
					$"Operation failed after {(elapsedTicks * 1000) / Stopwatch.Frequency} ms ({timeout} ms timeout).",
					lastException!
				);
			}

			// Very light natural jitter for better behavior in concurrent scenarios (no extra param)
			var sleepTime = delay;
			if (delay > 5)
			{
				// Tiny spread (±10%) without any Random allocation in most cases
				// -2 to +1 ms spread
				sleepTime = (int) ((delay + (Stopwatch.GetTimestamp() & 0x3)) - 2);
			}

			Thread.Sleep(sleepTime);
		}
	}

	public static bool WaitUntil(
		Func<bool> action,
		int timeout = 1000,
		int delay = 10,
		int minimum = 0,
		int maximum = 10000,
		IDateTimeProvider timeProvider = null,
		CancellationToken cancellationToken = default)
	{
		return WaitUntil<object>(null, _ => action(), timeout, delay, minimum, maximum, timeProvider, cancellationToken);
	}

	public static bool WaitUntil<T>(
		this T value,
		Func<T, bool> action,
		int timeout = 1000,
		int delay = 10,
		int minimum = 0,
		int maximum = 10000,
		IDateTimeProvider timeProvider = null,
		CancellationToken cancellationToken = default)
	{
		if (action is null)
		{
			throw new ArgumentNullException(nameof(action));
		}

		timeProvider ??= DateTimeProvider.RealTime;

		if (delay < 1)
		{
			delay = 1;
		}
		if (timeout < 0)
		{
			timeout = 0;
		}
		if (minimum < 0)
		{
			minimum = 0;
		}
		if (maximum < timeout)
		{
			maximum = timeout;
		}

		var start = timeProvider.UtcNow;
		var minTime = start.AddMilliseconds(minimum);
		var timeoutTime = start.AddMilliseconds(timeout);
		var maxTime = start.AddMilliseconds(maximum);

		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var now = timeProvider.UtcNow;

			if (now >= maxTime)
			{
				return false;
			}

			var conditionMet = action(value);

			if (conditionMet)
			{
				// Always enforce minimum wait before returning true
				if (now < minTime)
				{
					var remainingMin = (int) (minTime - now).TotalMilliseconds;
					if (remainingMin > 0)
					{
						cancellationToken.WaitHandle.WaitOne(remainingMin);
					}
				}
				return true;
			}

			// Condition not met yet
			if (now >= timeoutTime)
			{
				return false;
			}

			// Calculate safe sleep time
			var remainingToTimeout = (int) (timeoutTime - now).TotalMilliseconds;
			var remainingToMax = (int) (maxTime - now).TotalMilliseconds;
			var sleepTime = Math.Min(delay, Math.Min(remainingToTimeout, remainingToMax));

			if (sleepTime <= 0)
			{
				return false;
			}

			cancellationToken.WaitHandle.WaitOne(sleepTime);
		}
	}

	#endregion
}