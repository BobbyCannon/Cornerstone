#region References

using System;
using System.Threading;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Leading-edge throttle with a single trailing edge.
/// First <see cref="Trigger" /> in a window runs immediately.
/// Further triggers during the interval are coalesced into one trailing run
/// after the window ends (never drops the last request in a burst).
/// <see cref="Trigger" />(force: true) runs immediately and cancels any pending trailing edge,
/// unless the action is already on the stack — force does not nest.
/// </summary>
public sealed class Throttle : IDisposable
{
	#region Fields

	private readonly Action _action;
	private bool _busy;
	private readonly IDateTimeProvider _dateTimeProvider;
	private bool _disposed;
	private readonly TimeSpan _interval;
	private DateTime _lastExecuted;
	private readonly object _lock;
	private bool _trailingPending;
	private System.Threading.Timer _trailingTimer;
	private readonly bool _useRealTimeTimer;

	#endregion

	#region Constructors

	public Throttle(Action action, TimeSpan interval)
		: this(action, interval, DateTimeProvider.RealTime)
	{
	}

	public Throttle(Action action, TimeSpan interval, IDateTimeProvider dateTimeProvider)
	{
		_action = action ?? throw new ArgumentNullException(nameof(action));

		if (interval < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(interval));
		}

		_dateTimeProvider = dateTimeProvider ?? DateTimeProvider.RealTime;
		_useRealTimeTimer = ReferenceEquals(_dateTimeProvider, DateTimeProvider.RealTime);
		_lastExecuted = DateTime.MinValue;
		_lock = new();
		_interval = interval;
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			CancelTrailingUnsafe();
		}
	}

	/// <summary>
	/// Runs a pending trailing edge when the cooldown window has elapsed.
	/// Used by tests that advance <see cref="IDateTimeProvider" /> instead of waiting on a real timer.
	/// </summary>
	public void ProcessPending()
	{
		Action toRun = null;

		lock (_lock)
		{
			TryStartTrailingUnsafe(ref toRun);
		}

		InvokeAction(toRun);
	}

	/// <summary>
	/// Attempts to run the action.
	/// Leading edge when outside the cooldown window; otherwise schedules one trailing edge.
	/// </summary>
	/// <param name="force">
	/// When true the action runs immediately and any pending trailing edge is cancelled,
	/// unless the action is already running (sync, on the stack).
	/// </param>
	public void Trigger(bool force = false)
	{
		Action toRun = null;

		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			if (_busy)
			{
				// Do not nest. Coalesce into one run after the current action.
				_trailingPending = true;
				if (force)
				{
					_lastExecuted = DateTime.MinValue;
				}

				return;
			}

			var now = _dateTimeProvider.UtcNow;

			if (force || (_interval == TimeSpan.Zero))
			{
				CancelTrailingUnsafe();
				_lastExecuted = now;
				_busy = true;
				toRun = _action;
			}
			else if ((_lastExecuted == DateTime.MinValue) || ((now - _lastExecuted) >= _interval))
			{
				// Leading edge for this window.
				CancelTrailingUnsafe();
				_lastExecuted = now;
				_busy = true;
				toRun = _action;
			}
			else
			{
				// Cooldown: keep one trailing call so the burst is never dropped.
				_trailingPending = true;
				ScheduleTrailingUnsafe();
			}
		}

		InvokeAction(toRun);
	}

	private void CancelTrailingUnsafe()
	{
		_trailingPending = false;
		if (_trailingTimer is null)
		{
			return;
		}

		_trailingTimer.Dispose();
		_trailingTimer = null;
	}

	private void InvokeAction(Action toRun)
	{
		// Invoke outside the lock so re-entrant Trigger does not deadlock.
		if (toRun is null)
		{
			return;
		}

		try
		{
			toRun.Invoke();
		}
		finally
		{
			Action followUp = null;

			lock (_lock)
			{
				_busy = false;
				TryStartTrailingUnsafe(ref followUp);
			}

			if (followUp is not null)
			{
				InvokeAction(followUp);
			}
		}
	}

	private void OnTrailingTimer(object state)
	{
		ProcessPending();
	}

	private void ScheduleTrailingUnsafe()
	{
		if (!_useRealTimeTimer)
		{
			return;
		}

		if (_trailingTimer is not null)
		{
			// Already armed for the end of this window; extra hits stay coalesced.
			return;
		}

		var now = _dateTimeProvider.UtcNow;
		var remaining = _interval - (now - _lastExecuted);
		if (remaining < TimeSpan.Zero)
		{
			remaining = TimeSpan.Zero;
		}

		// One-shot timer; recreated only when a new cooldown window needs a trailing edge.
		_trailingTimer = new System.Threading.Timer(OnTrailingTimer, null, remaining, Timeout.InfiniteTimeSpan);
	}

	private void TryStartTrailingUnsafe(ref Action toRun)
	{
		if (_disposed || !_trailingPending || _busy)
		{
			return;
		}

		var now = _dateTimeProvider.UtcNow;
		if ((now - _lastExecuted) < _interval)
		{
			return;
		}

		if (_trailingTimer is not null)
		{
			_trailingTimer.Dispose();
			_trailingTimer = null;
		}

		_trailingPending = false;
		_lastExecuted = now;
		_busy = true;
		toRun = _action;
	}

	#endregion
}
