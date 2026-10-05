#region References

using System;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Nested startup timing scope. Dispose to record the sample. Default (null profiler) is a no-op.
/// </summary>
public readonly ref struct StartupScope : IDisposable
{
	#region Fields

	private readonly bool _accumulate;
	private readonly bool _isActive;

	private readonly StartupProfiler _profiler;
	private readonly long _startTicks;

	#endregion

	#region Constructors

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal StartupScope(StartupProfiler profiler, string name, long startTicks, bool accumulate = false)
	{
		_profiler = profiler;
		Name = name;
		_startTicks = startTicks;
		_accumulate = accumulate;
		_isActive = profiler != null;
	}

	#endregion

	#region Properties

	public string Name { get; }

	#endregion

	#region Methods

	public void Dispose()
	{
		if (!_isActive)
		{
			return;
		}

		if (_accumulate)
		{
			_profiler.OnAccumulateEnded(this, _startTicks);
			return;
		}

		_profiler.OnScopeEnded(this, _startTicks);
	}

	#endregion
}