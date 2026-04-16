#region References

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

public class Profiler : IEnumerable<TimedScopeStats>, IProfiler
{
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private long _lastRefreshTicks;
	private readonly ConcurrentDictionary<string, TimedScopeStats> _stats;

	#endregion

	#region Constructors

	public Profiler(IDateTimeProvider dateTimeProvider = null)
	{
		_dateTimeProvider = dateTimeProvider ?? DateTimeProvider.RealTime;
		_stats = new();
	}

	#endregion

	#region Methods

	public IEnumerator<TimedScopeStats> GetEnumerator()
	{
		return _stats.Values.GetEnumerator();
	}

	public long GetTicks()
	{
		return _dateTimeProvider.UtcNow.Ticks;
	}

	public void OnScopeEnded(TimedScope timedScope, long elapsedTicks)
	{
		// Hot path: GetOrAdd is amortized O(1), rare alloc only on first-seen method
		var stats = _stats.GetOrAdd(timedScope.Name, static _ => new TimedScopeStats());

		// Atomic adds: ~5-10 ns total, zero alloc/GC
		Interlocked.Add(ref stats.TotalTicks, elapsedTicks);
		Interlocked.Increment(ref stats.Count);
	}

	public void Refresh()
	{
		var timeStamp = _dateTimeProvider.UtcNow.Ticks;
		var intervalTicks = timeStamp - _lastRefreshTicks;

		if (intervalTicks <= 0)
		{
			_lastRefreshTicks = timeStamp;
			return;
		}

		var intervalSeconds = (double) intervalTicks / TimeSpan.TicksPerSecond;

		// Snapshot and compute
		foreach (var kvp in _stats)
		{
			var stats = kvp.Value;
			var total = Interlocked.Read(ref stats.TotalTicks);
			var count = Interlocked.Read(ref stats.Count);

			if (count == 0)
			{
				continue;
			}

			// Average duration
			var averageTicks = (double) total / count;
			var callsPerSeconds = count / intervalSeconds;

			// Atomic updates to "published" fields (safe for readers)
			Interlocked.Exchange(ref stats.AverageTicks, averageTicks);
			Interlocked.Exchange(ref stats.CallsPerSecond, callsPerSeconds);

			// Reset accumulators
			Interlocked.Exchange(ref stats.TotalTicks, 0);
			Interlocked.Exchange(ref stats.Count, 0);

			stats.AverageHistory?.Add(averageTicks);
			stats.PerSecondHistory?.Add(callsPerSeconds);
		}

		_lastRefreshTicks = timeStamp;
	}

	public (ISeriesDataProvider Average, ISeriesDataProvider PerSecond) SetupScopeHistory(string name, int size = 60)
	{
		var stats = _stats.GetOrAdd(name, static _ => new TimedScopeStats());
		stats.AverageHistory ??= new SeriesDataProvider(size);
		stats.PerSecondHistory ??= new SeriesDataProvider(size);
		return (stats.AverageHistory, stats.PerSecondHistory);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}

public interface IProfiler
{
	#region Methods

	long GetTicks();

	void OnScopeEnded(TimedScope timedScope, long elapsedTicks);

	#endregion
}