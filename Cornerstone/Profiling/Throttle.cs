#region References

using System;
using System.Threading;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Leading-edge throttle with a single trailing edge.
/// First <see cref="Trigger" /> in a window runs immediately.
/// Further triggers during the interval are coalesced into one trailing run
/// after the window ends (never drops the last request in a burst).
/// <see cref="Trigger" />(force: true) runs immediately and cancels any pending trailing edge.
/// </summary>
public sealed class Throttle : IDisposable
{
	#region Fields

	private readonly Action _action;
	private bool _disposed;
	private readonly TimeSpan _interval;
	private DateTime _lastExecuted;
	private readonly object _lock;
	private bool _trailingPending;
	private System.Threading.Timer _trailingTimer;

	#endregion

	#region Constructors

	public Throttle(Action action, TimeSpan interval)
	{
		_action = action ?? throw new ArgumentNullException(nameof(action));

		if (interval < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(interval));
		}

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
	/// Attempts to run the action.
	/// Leading edge when outside the cooldown window; otherwise schedules one trailing edge.
	/// </summary>
	/// <param name="force"> When true the action runs immediately and any pending trailing edge is cancelled. </param>
	public void Trigger(bool force = false)
	{
		Action toRun = null;

		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			var now = DateTime.UtcNow;

			if (force || (_interval == TimeSpan.Zero))
			{
				CancelTrailingUnsafe();
				_lastExecuted = now;
				toRun = _action;
			}
			else if ((_lastExecuted == DateTime.MinValue) || ((now - _lastExecuted) >= _interval))
			{
				// Leading edge for this window.
				CancelTrailingUnsafe();
				_lastExecuted = now;
				toRun = _action;
			}
			else
			{
				// Cooldown: keep one trailing call so the burst is never dropped.
				_trailingPending = true;
				ScheduleTrailingUnsafe(now);
			}
		}

		// Invoke outside the lock so re-entrant Trigger does not deadlock.
		toRun?.Invoke();
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

	private void OnTrailingTimer(object state)
	{
		Action toRun = null;

		lock (_lock)
		{
			if (_trailingTimer is not null)
			{
				_trailingTimer.Dispose();
				_trailingTimer = null;
			}

			if (_disposed || !_trailingPending)
			{
				return;
			}

			_trailingPending = false;
			_lastExecuted = DateTime.UtcNow;
			toRun = _action;
		}

		toRun?.Invoke();
	}

	private void ScheduleTrailingUnsafe(DateTime now)
	{
		if (_trailingTimer is not null)
		{
			// Already armed for the end of this window; extra hits stay coalesced.
			return;
		}

		var remaining = _interval - (now - _lastExecuted);
		if (remaining < TimeSpan.Zero)
		{
			remaining = TimeSpan.Zero;
		}

		// One-shot timer; recreated only when a new cooldown window needs a trailing edge.
		_trailingTimer = new System.Threading.Timer(OnTrailingTimer, null, remaining, Timeout.InfiniteTimeSpan);
	}

	#endregion
}