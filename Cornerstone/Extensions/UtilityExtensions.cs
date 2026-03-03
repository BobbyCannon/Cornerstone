#region References

using System;
using System.Diagnostics;
using System.Threading;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// General utility extensions
/// </summary>
public static class UtilityExtensions
{
	#region Methods

	/// <summary>
	/// Continues to run the action until we hit the timeout. If an exception occurs then delay for the
	/// provided delay time.
	/// </summary>
	/// <param name="action"> The action to attempt to retry. </param>
	/// <param name="timeout"> The timeout to attempt the action. This value is in milliseconds. </param>
	/// <param name="delay"> The delay in between actions. This value is in milliseconds. </param>
	/// <returns> The response from the action. </returns>
	public static void Retry(Action action, TimeSpan timeout, TimeSpan delay)
	{
		var stopwatch = Stopwatch.StartNew();

		while (true)
		{
			Exception lastException;

			try
			{
				action();
				return;
			}
			catch (Exception ex)
			{
				lastException = ex;
			}

			var elapsed = stopwatch.Elapsed;
			if (elapsed >= timeout)
			{
				throw new AggregateException($"Operation failed after {stopwatch.ElapsedMilliseconds} ms ({timeout.TotalMilliseconds:F0} ms timeout).", lastException!);
			}

			Thread.Sleep(delay);
		}
	}

	public static bool WaitUntil(
		Func<bool> action,
		TimeSpan? timeout = null,
		TimeSpan? delay = null,
		TimeSpan minimum = default,
		TimeSpan? maximum = null,
		IDateTimeProvider timeProvider = null,
		CancellationToken cancellationToken = default)
	{
		return WaitUntil<object>(null, _ => action(), timeout, delay, minimum, maximum, timeProvider, cancellationToken);
	}

	public static bool WaitUntil(
		this object value,
		Func<bool> action,
		TimeSpan? timeout = null,
		TimeSpan? delay = null,
		TimeSpan minimum = default,
		TimeSpan? maximum = null,
		IDateTimeProvider timeProvider = null,
		CancellationToken cancellationToken = default)
	{
		return WaitUntil<object>(null, _ => action(), timeout, delay, minimum, maximum, timeProvider, cancellationToken);
	}

	public static bool WaitUntil<T>(
		this T value,
		Func<T, bool> action,
		TimeSpan? timeout = null,
		TimeSpan? delay = null,
		TimeSpan minimum = default,
		TimeSpan? maximum = null,
		IDateTimeProvider timeProvider = null,
		CancellationToken cancellationToken = default)
	{
		timeProvider ??= DateTimeProvider.RealTime;
		timeout ??= TimeSpan.FromSeconds(1);
		delay ??= TimeSpan.FromMilliseconds(1);
		maximum ??= timeout.Value.Add(TimeSpan.FromSeconds(10));

		if (delay <= TimeSpan.Zero)
		{
			delay = TimeSpan.FromMilliseconds(1);
		}

		// Fast path
		if (action(value))
		{
			return true;
		}

		var start = timeProvider.UtcNow;

		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var now = timeProvider.UtcNow;
			var elapsed = now - start;

			if (elapsed >= maximum)
			{
				return false;
			}

			if (action(value))
			{
				// still enforce minimum wait if requested
				var remainingMin = minimum - elapsed;
				if (remainingMin > TimeSpan.Zero)
				{
					cancellationToken.WaitHandle.WaitOne(remainingMin);
				}
				return true;
			}

			if (elapsed >= timeout)
			{
				return false;
			}

			// Sleep — but respect both timeout and cancellation
			var sleepTime = TimeSpanExtensions.Min(delay.Value, timeout.Value - elapsed);
			sleepTime = TimeSpanExtensions.Min(sleepTime, maximum.Value - elapsed);

			if (sleepTime <= TimeSpan.Zero)
			{
				return false;
			}

			cancellationToken.WaitHandle.WaitOne(sleepTime);
		}
	}

	#endregion
}