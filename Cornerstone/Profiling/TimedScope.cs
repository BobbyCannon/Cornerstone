#region References

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

public readonly ref struct TimedScope : IDisposable
{
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly IProfiler _profiler;
	private readonly long _startTimestamp;

	#endregion

	#region Constructors

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TimedScope(string name, IProfiler profiler, IDateTimeProvider dateTimeProvider)
	{
		Name = name;
		_profiler = profiler;
		_dateTimeProvider = dateTimeProvider;
		_startTimestamp = dateTimeProvider?.Now.Ticks ?? Stopwatch.GetTimestamp();
	}

	#endregion

	#region Properties

	public string Name { get; init; }

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_startTimestamp == 0)
		{
			return;
		}

		var endTimestamp = _dateTimeProvider?.Now.Ticks ?? Stopwatch.GetTimestamp();
		var elapsedTicks = endTimestamp - _startTimestamp;
		_profiler?.OnScopeEnded(this, elapsedTicks);
	}

	#endregion
}