#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// One-shot hierarchical startup timing session. Not for continuous rate metrics — use <see cref="Profiler" /> for those.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class StartupProfiler
{
	#region Constants

	public const string RootName = "ApplicationStartup";
	public const double SlowestThresholdMilliseconds = 100;
	public const double SlowThresholdMilliseconds = 25;
	public const string UnknownName = "Unknown";

	#endregion

	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private bool _hasMark;
	private string _markName;
	private long _markTicks;
	private readonly Stack<OpenFrame> _stack;
	private readonly DateTime _startTime;
	private readonly long _startTicks;
	private readonly List<StartupSample> _topLevelSamples;

	#endregion

	#region Constructors

	/// <summary>
	/// Create a session using the provided time source (defaults to <see cref="DateTimeProvider.RealTime" />).
	/// </summary>
	public StartupProfiler(IDateTimeProvider dateTimeProvider = null)
	{
		_dateTimeProvider = dateTimeProvider ?? DateTimeProvider.RealTime;
		_startTime = _dateTimeProvider.UtcNow;
		_startTicks = _startTime.Ticks;
		_stack = new Stack<OpenFrame>(8);
		_topLevelSamples = new List<StartupSample>(16);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Wall time since session construction (or until <see cref="Complete" /> froze the clock).
	/// </summary>
	public TimeSpan Elapsed
	{
		get
		{
			if (IsCompleted && (Root != null))
			{
				return Root.Elapsed;
			}

			return TimeSpan.FromTicks(Math.Max(0, GetTicks() - _startTicks));
		}
	}

	public bool IsCompleted { get; private set; }

	/// <summary>
	/// Frozen root sample after <see cref="Complete" />; null while the session is open.
	/// </summary>
	public StartupSample Root { get; private set; }

	/// <summary>
	/// UTC time when this session was constructed (not a timed sample).
	/// </summary>
	public DateTime StartTime => _startTime;

	/// <summary>
	/// Top-level samples recorded so far (before complete) or root children (after complete).
	/// </summary>
	public IReadOnlyList<StartupSample> Samples =>
		IsCompleted && (Root != null)
			? Root.Children
			: _topLevelSamples;

	#endregion

	#region Methods

	/// <summary>
	/// Freeze the session, attach residual <see cref="UnknownName" /> if wall time exceeds accounted top-level scopes, and build <see cref="Root" />.
	/// Idempotent.
	/// </summary>
	public void Complete()
	{
		if (IsCompleted)
		{
			return;
		}

		// Unbalanced scopes: close with current time so a report is still useful.
		while (_stack.Count > 0)
		{
			var frame = _stack.Peek();
			EndScope(frame.Name, frame.StartTicks);
		}

		var total = TimeSpan.FromTicks(Math.Max(0, GetTicks() - _startTicks));
		var accountedTicks = 0L;
		for (var i = 0; i < _topLevelSamples.Count; i++)
		{
			accountedTicks += _topLevelSamples[i].Elapsed.Ticks;
		}

		var residualTicks = total.Ticks - accountedTicks;
		if (residualTicks > 0)
		{
			var unknownOffset = TimeSpan.FromTicks(Math.Max(0, accountedTicks));
			_topLevelSamples.Add(new StartupSample(
				UnknownName,
				0,
				unknownOffset,
				TimeSpan.FromTicks(residualTicks)
			));
		}

		// Freeze children list
		var children = _topLevelSamples.Count == 0
			? Array.Empty<StartupSample>()
			: _topLevelSamples.ToArray();

		Root = new StartupSample(RootName, -1, TimeSpan.Zero, total, children);
		IsCompleted = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public long GetTicks()
	{
		return _dateTimeProvider.UtcNow.Ticks;
	}

	/// <summary>
	/// Remember now as the start of a later RecordMark. One pending mark at a time.
	/// </summary>
	public void Mark(string name)
	{
		if (IsCompleted || string.IsNullOrEmpty(name))
		{
			return;
		}

		_markName = name;
		_markTicks = GetTicks();
		_hasMark = true;
	}

	/// <summary>
	/// Close the pending Mark as a sample named when Mark was called.
	/// </summary>
	public void RecordMark()
	{
		if (!_hasMark || string.IsNullOrEmpty(_markName))
		{
			return;
		}

		var name = _markName;
		var startTicks = _markTicks;
		_markName = null;
		_markTicks = 0;
		_hasMark = false;
		Record(name, startTicks);
	}

	public void Time(string name, Action action)
	{
		using (BeginScope(name))
		{
			action();
		}
	}

	public T Time<T>(string name, Func<T> action)
	{
		using (BeginScope(name))
		{
			return action();
		}
	}

	/// <summary>
	/// Record a span that started at startTicks (UtcNow ticks). Use when a ref struct scope cannot be stored across methods.
	/// </summary>
	public void Record(string name, long startTicks)
	{
		if (IsCompleted || string.IsNullOrEmpty(name) || (startTicks < 0))
		{
			return;
		}

		var endTicks = GetTicks();
		if (endTicks < startTicks)
		{
			endTicks = startTicks;
		}

		var elapsed = TimeSpan.FromTicks(endTicks - startTicks);
		var offset = TimeSpan.FromTicks(Math.Max(0, startTicks - _startTicks));
		var depth = _stack.Count;
		var sample = new StartupSample(name, depth, offset, elapsed);

		if (_stack.Count == 0)
		{
			_topLevelSamples.Add(sample);
		}
		else
		{
			_stack.Peek().Children.Add(sample);
		}
	}

	/// <summary>
	/// Inclusive elapsed cutoff for a detail level. <see cref="StartupProfileDetail.All" /> is zero (keep everything).
	/// </summary>
	public static TimeSpan ThresholdFor(StartupProfileDetail detail)
	{
		return detail switch
		{
			StartupProfileDetail.Slowest => TimeSpan.FromMilliseconds(SlowestThresholdMilliseconds),
			StartupProfileDetail.Slow => TimeSpan.FromMilliseconds(SlowThresholdMilliseconds),
			_ => TimeSpan.Zero
		};
	}

	/// <summary>
	/// Human-readable hierarchical report (milliseconds).
	/// </summary>
	public string ToReport()
	{
		return ToReport(TimeSpan.Zero);
	}

	/// <summary>
	/// Human-readable hierarchical report. Nodes below <paramref name="minimumElapsed" /> are omitted unless they
	/// sit on the path to a node that meets the cutoff. Does not freeze the session — call <see cref="Complete" />
	/// explicitly. Debugger ToString / watches must not complete the capture.
	/// </summary>
	public string ToReport(TimeSpan minimumElapsed)
	{
		var root = SnapshotRoot();
		var writer = new AsciiWriter(512);
		writer.AppendTree(
			root,
			static sample => sample.Children,
			(sample, parent) => FormatSampleLine(sample, parent?.Elapsed ?? sample.Elapsed, _startTime),
			sample => KeepSample(sample, minimumElapsed));
		return writer.ToString();
	}

	public string ToReport(StartupProfileDetail detail)
	{
		return ToReport(ThresholdFor(detail));
	}

	public override string ToString()
	{
		return ToReport();
	}

	/// <summary>
	/// Begin a nested scope. Prefer <see cref="StartupProfilerExtensions.Start" /> for null-safe call sites.
	/// No-op after <see cref="Complete" />.
	/// </summary>
	internal StartupScope BeginScope(string name)
	{
		if (IsCompleted || string.IsNullOrEmpty(name))
		{
			return default;
		}

		var startTicks = GetTicks();
		var children = new List<StartupSample>(4);
		_stack.Push(new OpenFrame(name, startTicks, children));
		return new StartupScope(this, name, startTicks);
	}

	/// <summary>
	/// Begin an exclusive timed span that is merged by name into the current open <see cref="Start" /> scope.
	/// No-op when the session is complete or no scope is open.
	/// </summary>
	internal StartupScope BeginAccumulate(string name)
	{
		if (IsCompleted || string.IsNullOrEmpty(name) || (_stack.Count == 0))
		{
			return default;
		}

		return new StartupScope(this, name, GetTicks(), accumulate: true);
	}

	/// <summary>
	/// Called by <see cref="StartupScope.Dispose" />.
	/// </summary>
	internal void OnScopeEnded(StartupScope scope, long startTicks)
	{
		EndScope(scope.Name, startTicks);
	}

	internal void OnAccumulateEnded(StartupScope scope, long startTicks)
	{
		if (IsCompleted || (_stack.Count == 0) || string.IsNullOrEmpty(scope.Name))
		{
			return;
		}

		var endTicks = GetTicks();
		if (endTicks < startTicks)
		{
			endTicks = startTicks;
		}

		_stack.Peek().AddAccumulated(scope.Name, endTicks - startTicks);
	}

	public static bool KeepSample(StartupSample sample, TimeSpan minimumElapsed)
	{
		if (sample == null)
		{
			return false;
		}

		if (minimumElapsed.Ticks <= 0)
		{
			return true;
		}

		if (sample.Elapsed >= minimumElapsed)
		{
			return true;
		}

		for (var i = 0; i < sample.Children.Count; i++)
		{
			if (KeepSample(sample.Children[i], minimumElapsed))
			{
				return true;
			}
		}

		return false;
	}

	private string DebuggerDisplay =>
		$"{RootName} {Elapsed.TotalMilliseconds:0.0} ms, {Samples.Count} samples, {(IsCompleted ? "completed" : "open")}";

	/// <summary>
	/// Current tree without mutating state. Open scopes appear with elapsed-so-far. No Unknown residual until <see cref="Complete" />.
	/// </summary>
	private StartupSample SnapshotRoot()
	{
		if (IsCompleted && (Root != null))
		{
			return Root;
		}

		var now = GetTicks();
		var total = TimeSpan.FromTicks(Math.Max(0, now - _startTicks));
		var frames = _stack.ToArray();
		StartupSample open = null;

		for (var i = 0; i < frames.Length; i++)
		{
			var frame = frames[i];
			var depth = frames.Length - 1 - i;
			var elapsed = TimeSpan.FromTicks(Math.Max(0, now - frame.StartTicks));
			var offset = TimeSpan.FromTicks(Math.Max(0, frame.StartTicks - _startTicks));
			var accumulated = frame.CopyAccumulated(depth + 1, offset);
			var childCount = accumulated.Length + frame.Children.Count + (open != null ? 1 : 0);
			var children = childCount == 0
				? Array.Empty<StartupSample>()
				: new StartupSample[childCount];

			var index = 0;
			for (var c = 0; c < accumulated.Length; c++)
			{
				children[index++] = accumulated[c];
			}

			for (var c = 0; c < frame.Children.Count; c++)
			{
				children[index++] = frame.Children[c];
			}

			if (open != null)
			{
				children[index] = open;
			}

			open = new StartupSample(frame.Name, depth, offset, elapsed, children);
		}

		var topCount = _topLevelSamples.Count + (open != null ? 1 : 0);
		var top = topCount == 0
			? Array.Empty<StartupSample>()
			: new StartupSample[topCount];

		for (var i = 0; i < _topLevelSamples.Count; i++)
		{
			top[i] = _topLevelSamples[i];
		}

		if (open != null)
		{
			top[topCount - 1] = open;
		}

		return new StartupSample(RootName, -1, TimeSpan.Zero, total, top);
	}

	private static string FormatSampleLine(StartupSample sample, TimeSpan parentElapsed, DateTime startTime)
	{
		var ms = sample.Elapsed.TotalMilliseconds;
		var percent = parentElapsed.Ticks > 0
			? (100.0 * sample.Elapsed.Ticks) / parentElapsed.Ticks
			: 100.0;

		if (sample.Depth < 0)
		{
			return sample.Name
				+ " "
				+ ms.ToString("0.0")
				+ " ms  ("
				+ percent.ToString("0.0")
				+ "%)  at "
				+ startTime.ToString("O");
		}

		return sample.Name
			+ " "
			+ ms.ToString("0.0")
			+ " ms  ("
			+ percent.ToString("0.0")
			+ "%)";
	}

	private void EndScope(string name, long startTicks)
	{
		if (IsCompleted || (_stack.Count == 0))
		{
			return;
		}

		var frame = _stack.Pop();

		// Prefer the stack frame name if dispose order mismatched.
		var sampleName = frame.Name ?? name;
		var endTicks = GetTicks();
		var scopeStart = frame.StartTicks;
		if (endTicks < scopeStart)
		{
			endTicks = scopeStart;
		}

		var elapsed = TimeSpan.FromTicks(endTicks - scopeStart);
		var offset = TimeSpan.FromTicks(Math.Max(0, scopeStart - _startTicks));
		var depth = _stack.Count;
		var accumulated = frame.CopyAccumulated(depth + 1, offset);
		IReadOnlyList<StartupSample> children;
		if ((accumulated.Length == 0) && (frame.Children.Count == 0))
		{
			children = Array.Empty<StartupSample>();
		}
		else if (accumulated.Length == 0)
		{
			children = frame.Children.ToArray();
		}
		else
		{
			var merged = new StartupSample[accumulated.Length + frame.Children.Count];
			accumulated.CopyTo(merged, 0);
			frame.Children.CopyTo(merged, accumulated.Length);
			children = merged;
		}

		var sample = new StartupSample(sampleName, depth, offset, elapsed, children);

		if (_stack.Count == 0)
		{
			_topLevelSamples.Add(sample);
		}
		else
		{
			_stack.Peek().Children.Add(sample);
		}
	}

	#endregion

	#region Classes

	private sealed class OpenFrame
	{
		#region Fields

		private List<(string Name, long Ticks)> _accumulated;

		#endregion

		#region Constructors

		public OpenFrame(string name, long startTicks, List<StartupSample> children)
		{
			Name = name;
			StartTicks = startTicks;
			Children = children;
		}

		#endregion

		#region Properties

		public List<StartupSample> Children { get; }
		public string Name { get; }
		public long StartTicks { get; }

		#endregion

		#region Methods

		public void AddAccumulated(string name, long ticks)
		{
			if (ticks < 0)
			{
				return;
			}

			_accumulated ??= new List<(string, long)>(4);
			for (var i = 0; i < _accumulated.Count; i++)
			{
				if (_accumulated[i].Name == name)
				{
					_accumulated[i] = (name, _accumulated[i].Ticks + ticks);
					return;
				}
			}

			_accumulated.Add((name, ticks));
		}

		public StartupSample[] CopyAccumulated(int depth, TimeSpan parentOffset)
		{
			if ((_accumulated == null) || (_accumulated.Count == 0))
			{
				return Array.Empty<StartupSample>();
			}

			var samples = new StartupSample[_accumulated.Count];
			for (var i = 0; i < _accumulated.Count; i++)
			{
				var entry = _accumulated[i];
				samples[i] = new StartupSample(
					entry.Name,
					depth,
					parentOffset,
					TimeSpan.FromTicks(entry.Ticks));
			}

			return samples;
		}

		#endregion
	}

	#endregion
}