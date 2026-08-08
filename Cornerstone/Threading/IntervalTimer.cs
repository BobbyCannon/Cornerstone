#region References

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Threading;

public sealed class IntervalTimer : IDisposable
{
	#region Fields

	private bool _disposed;
	private readonly bool _isConstrainedPlatform;
	private long _nextTarget;
	private readonly long _periodTicks;

	// How long we are willing to spin tightly before yielding on constrained platforms
	private static readonly long _yieldThresholdTicks;

	#endregion

	#region Constructors

	public IntervalTimer(TimeSpan period)
	{
		if (period <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(period));
		}

		_periodTicks = (long) (period.TotalSeconds * Stopwatch.Frequency);

		_isConstrainedPlatform =
			OperatingSystem.IsBrowser()
			|| OperatingSystem.IsAndroid()
			|| OperatingSystem.IsIOS()
			|| OperatingSystem.IsTvOS()
			|| OperatingSystem.IsWatchOS()
			|| (Environment.ProcessorCount == 1);

		_nextTarget = Stopwatch.GetTimestamp() + _periodTicks;
	}

	static IntervalTimer()
	{
		// ≈ 0.4 ms
		_yieldThresholdTicks = (long) ((0.4 * Stopwatch.Frequency) / 1000.0);
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		_disposed = true;
	}

	public async ValueTask<bool> WaitForNextTickAsync(CancellationToken ct = default)
	{
		if (_disposed)
		{
			return false;
		}

		while (true)
		{
			ct.ThrowIfCancellationRequested();

			var remaining = _nextTarget - Stopwatch.GetTimestamp();
			if (remaining <= 0)
			{
				break;
			}

			if (_isConstrainedPlatform)
			{
				// Keep the original hybrid for browser / mobile / single-core
				if (remaining > _yieldThresholdTicks)
				{
					await Task.Yield();
				}
				else
				{
					Thread.SpinWait(8);
				}
			}
			else
			{
				// Only sleep when we have a comfortable amount of time left
				var sleepThreshold = Stopwatch.Frequency / 50; // ≈ 20 ms
				var yieldThreshold = Stopwatch.Frequency / 2000; // ≈ 0.5 ms

				if (remaining > sleepThreshold)
				{
					// Leave a bigger safety margin so we don’t overshoot
					var sleepMs = (int) (((remaining - sleepThreshold) * 1000.0) / Stopwatch.Frequency);
					if (sleepMs > 0)
					{
						Thread.Sleep(sleepMs);
					}
				}
				else if (remaining > yieldThreshold)
				{
					Thread.Sleep(0); // or await Task.Yield()
				}
				else
				{
					Thread.SpinWait(1);
				}
			}
		}

		// … existing catch-up logic …
		_nextTarget += _periodTicks;
		var now = Stopwatch.GetTimestamp();
		if (_nextTarget < now)
		{
			_nextTarget = now + _periodTicks;
		}

		return true;
	}

	#endregion
}