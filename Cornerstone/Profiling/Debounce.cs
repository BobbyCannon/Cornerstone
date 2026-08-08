#region References

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Simple trailing debounce (fire-and-forget, no value queue).
/// Trigger() starts / resets a timer.
/// The action runs only after the interval of silence.
/// Trigger(true) cancels any pending run and executes immediately.
/// </summary>
public sealed class Debounce : IDisposable
{
	#region Fields

	private readonly Action _action;
	private CancellationTokenSource _cts;
	private bool _disposed;
	private readonly TimeSpan _interval;
	private readonly object _lock;

	#endregion

	#region Constructors

	public Debounce(Action action, TimeSpan interval)
	{
		_action = action ?? throw new ArgumentNullException(nameof(action));

		if (interval < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(interval));
		}

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
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;
		}
	}

	public void Trigger(bool force = false)
	{
		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			// Cancel any previously scheduled run
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;

			if (force)
			{
				_action();
				return;
			}

			// Schedule a new run
			_cts = new CancellationTokenSource();
			var token = _cts.Token;

			_ = Task
				.Delay(_interval, token)
				.ContinueWith(t =>
					{
						if (t.IsCanceled || _disposed)
						{
							return;
						}
						_action();
					},
					TaskContinuationOptions.OnlyOnRanToCompletion
				);
		}
	}

	#endregion
}