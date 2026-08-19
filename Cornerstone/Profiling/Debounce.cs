#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Simple trailing debounce (fire-and-forget, no value queue).
/// Trigger() starts / resets a timer.
/// The action runs only after the interval of silence.
/// Trigger(true) cancels any pending run and executes immediately
/// (or immediately after the current run).
/// At most one action is in flight at a time.
/// </summary>
public sealed class Debounce : IDisposable
{
	#region Fields

	private readonly Action _action;
	private CancellationTokenSource _cts;
	private readonly IDateTimeProvider _dateTimeProvider;
	private DateTime _dueAt;
	private bool _disposed;
	private bool _inFlight;
	private readonly TimeSpan _interval;
	private readonly object _lock;
	private bool _runAgain;
	private bool _scheduled;
	private readonly bool _useRealTimeDelay;

	#endregion

	#region Constructors

	public Debounce(Action action, TimeSpan interval)
		: this(action, interval, DateTimeProvider.RealTime)
	{
	}

	public Debounce(Action action, TimeSpan interval, IDateTimeProvider dateTimeProvider)
	{
		_action = action ?? throw new ArgumentNullException(nameof(action));

		if (interval < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(interval));
		}

		_lock = new();
		_interval = interval;
		_dateTimeProvider = dateTimeProvider ?? DateTimeProvider.RealTime;
		_useRealTimeDelay = ReferenceEquals(_dateTimeProvider, DateTimeProvider.RealTime);
		_dueAt = DateTime.MinValue;
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
			_runAgain = false;
			_scheduled = false;
			CancelPendingUnsafe();
		}
	}

	/// <summary>
	/// Runs a pending action when the silence interval has elapsed.
	/// Used by tests that advance <see cref="IDateTimeProvider" /> instead of waiting on a real delay.
	/// </summary>
	public void ProcessPending()
	{
		var shouldRun = false;

		lock (_lock)
		{
			if (_disposed || !_scheduled)
			{
				return;
			}

			if (_dateTimeProvider.UtcNow < _dueAt)
			{
				return;
			}

			_scheduled = false;
			CancelPendingUnsafe();
			shouldRun = TryBeginOrCoalesceUnsafe();
		}

		if (shouldRun)
		{
			RunLoop();
		}
	}

	public void Trigger(bool force = false)
	{
		var shouldRun = false;

		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			// Cancel any previously scheduled run
			CancelPendingUnsafe();
			_scheduled = false;

			if (force)
			{
				shouldRun = TryBeginOrCoalesceUnsafe();
			}
			else
			{
				_dueAt = _dateTimeProvider.UtcNow + _interval;
				_scheduled = true;

				if (_useRealTimeDelay)
				{
					_cts = new CancellationTokenSource();
					var token = _cts.Token;

					_ = Task
						.Delay(_interval, token)
						.ContinueWith(t =>
							{
								if (t.IsCanceled)
								{
									return;
								}

								ProcessPending();
							},
							TaskContinuationOptions.OnlyOnRanToCompletion
						);
				}
			}
		}

		if (shouldRun)
		{
			RunLoop();
		}
	}

	private void CancelPendingUnsafe()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;
	}

	private void RunLoop()
	{
		while (true)
		{
			// Invoke outside the lock so re-entrant Trigger does not deadlock.
			_action();

			var continueRun = false;

			lock (_lock)
			{
				if (_disposed)
				{
					_inFlight = false;
					_runAgain = false;
					return;
				}

				if (_runAgain)
				{
					_runAgain = false;
					continueRun = true;
				}
				else
				{
					_inFlight = false;
				}
			}

			if (!continueRun)
			{
				return;
			}
		}
	}

	private bool TryBeginOrCoalesceUnsafe()
	{
		if (_inFlight)
		{
			_runAgain = true;
			return false;
		}

		_inFlight = true;
		return true;
	}

	#endregion
}
