#region References

using System;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Profiling;

public readonly ref struct TimedScope : IDisposable
{
	#region Fields

	private readonly IProfiler _profiler;
	private readonly long _startTicks;

	#endregion

	#region Constructors

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TimedScope(string name, IProfiler profiler)
	{
		Name = name;
		_profiler = profiler;
		_startTicks = profiler.GetTicks();
	}

	#endregion

	#region Properties

	public string Name { get; init; }

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_startTicks == 0)
		{
			return;
		}

		var endTicks = _profiler.GetTicks();
		_profiler?.OnScopeEnded(this, endTicks - _startTicks);
	}

	#endregion
}