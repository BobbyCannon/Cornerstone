#region References

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// One-shot hierarchical startup timing session. Not for continuous rate metrics — use <see cref="Profiler" /> for those.
/// </summary>
public sealed class StartupProfiler
{
	#region Constants

	public const string RootName = "ApplicationStartup";
	public const string UnknownName = "Unknown";

	#endregion

	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly Stack<OpenFrame> _stack;
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
		_startTicks = GetTicks();
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
	/// Human-readable hierarchical report (milliseconds).
	/// </summary>
	public string ToReport()
	{
		if (!IsCompleted)
		{
			Complete();
		}

		var builder = new StringBuilder(512);
		AppendSample(builder, Root, Root.Elapsed);
		return builder.ToString();
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
	/// Called by <see cref="StartupScope.Dispose" />.
	/// </summary>
	internal void OnScopeEnded(StartupScope scope, long startTicks)
	{
		EndScope(scope.Name, startTicks);
	}

	private static void AppendSample(StringBuilder builder, StartupSample sample, TimeSpan parentElapsed)
	{
		if (sample == null)
		{
			return;
		}

		var indent = sample.Depth < 0
			? string.Empty
			: new string(' ', (sample.Depth + 1) * 2);

		var connector = sample.Depth < 0 ? string.Empty : "├── ";
		var ms = sample.Elapsed.TotalMilliseconds;
		var percent = parentElapsed.Ticks > 0
			? (100.0 * sample.Elapsed.Ticks) / parentElapsed.Ticks
			: 100.0;

		if (sample.Depth < 0)
		{
			builder.Append(sample.Name)
				.Append(' ')
				.Append(ms.ToString("0.0"))
				.Append(" ms  (")
				.Append(percent.ToString("0.0"))
				.AppendLine("%)");
		}
		else
		{
			builder.Append(indent)
				.Append(connector)
				.Append(sample.Name)
				.Append(' ')
				.Append(ms.ToString("0.0"))
				.Append(" ms  (")
				.Append(percent.ToString("0.0"))
				.AppendLine("%)");
		}

		var childParent = sample.Elapsed;
		for (var i = 0; i < sample.Children.Count; i++)
		{
			AppendSample(builder, sample.Children[i], childParent);
		}
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
		var children = frame.Children.Count == 0
			? Array.Empty<StartupSample>()
			: frame.Children.ToArray();

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
	}

	#endregion
}