#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Protocol;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// Whether the editor host is loading metadata or answering a request.
/// The status-bar gear reads <see cref="IsWorking"/>. The processes window reads <see cref="StatusText"/>.
/// </summary>
internal static class EditorHostActivity
{
	#region Fields

	private static readonly object Gate;
	private static int _depth;
	private static int _index;
	private static string _message;
	private static string _name;
	private static string _phase;
	private static int _stopGeneration;
	private static int _total;
	private static int _visible;
	private static Action _changed;

	#endregion

	#region Constructors

	static EditorHostActivity()
	{
		Gate = new object();
		_depth = 0;
		_index = 0;
		_message = string.Empty;
		_name = string.Empty;
		_phase = string.Empty;
		_stopGeneration = 0;
		_total = 0;
		_visible = 0;
		_changed = null;
	}

	#endregion

	#region Properties

	public static bool IsWorking
	{
		get { return Volatile.Read(ref _visible) != 0; }
	}

	public static string StatusText
	{
		get
		{
			lock (Gate)
			{
				if (_total > 0)
				{
					return MetadataProgressText.Format(_phase, _name, _index, _total);
				}

				if (!string.IsNullOrEmpty(_message))
				{
					return _message;
				}
			}

			if (IsWorking)
			{
				return "Cornerstone is loading";
			}

			return "Idle";
		}
	}

	#endregion

	#region Events

	public static event Action Changed
	{
		add { _changed += value; }
		remove { _changed -= value; }
	}

	#endregion

	#region Methods

	public static void Report(string phase, string name, int index, int total)
	{
		lock (Gate)
		{
			_phase = phase ?? string.Empty;
			_name = name ?? string.Empty;
			_index = index;
			_total = total;
			_message = string.Empty;
		}

		Raise();
		if (Volatile.Read(ref _depth) > 0)
		{
			return;
		}

		ScheduleIdle();
	}

	public static void SetMessage(string message)
	{
		lock (Gate)
		{
			_message = message ?? string.Empty;
			_total = 0;
		}

		Raise();
	}

	public static void Enter()
	{
		Interlocked.Increment(ref _stopGeneration);
		Interlocked.Increment(ref _depth);
		if (Interlocked.Exchange(ref _visible, 1) == 0)
		{
			Raise();
		}
	}

	public static void Exit()
	{
		var depth = Interlocked.Decrement(ref _depth);
		if (depth > 0)
		{
			return;
		}

		if (depth < 0)
		{
			Interlocked.Exchange(ref _depth, 0);
		}

		ScheduleIdle();
	}

	private static void ScheduleIdle()
	{
		var generation = Interlocked.Increment(ref _stopGeneration);
		Task.Delay(TimeSpan.FromMilliseconds(400)).ContinueWith(
			_ => FinishIdle(generation),
			TaskScheduler.Default);
	}

	private static void FinishIdle(int generation)
	{
		if (generation != Volatile.Read(ref _stopGeneration))
		{
			return;
		}

		if (Volatile.Read(ref _depth) > 0)
		{
			return;
		}

		var changed = false;
		lock (Gate)
		{
			if ((_total != 0) || !string.IsNullOrEmpty(_message) || !string.IsNullOrEmpty(_name))
			{
				_message = string.Empty;
				_phase = string.Empty;
				_name = string.Empty;
				_index = 0;
				_total = 0;
				changed = true;
			}
		}

		if (Interlocked.Exchange(ref _visible, 0) == 1)
		{
			changed = true;
		}

		if (changed)
		{
			Raise();
		}
	}

	private static void Raise()
	{
		var changed = _changed;
		if (changed != null)
		{
			changed();
		}
	}

	#endregion
}
