#region References

using System;
using System.Windows.Threading;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// Classic debounce: each <see cref="Queue"/> restarts the timer; the last value
/// is delivered once the interval elapses with no further calls.
/// </summary>
public class Throttle<T> : IDisposable
{
	#region Fields

	private readonly Action<T> _execute;
	private readonly DispatcherTimer _timer;
	private T _value;

	#endregion

	#region Constructors

	public Throttle(TimeSpan interval, Action<T> execute)
	{
		_execute = execute;

		_timer = new DispatcherTimer
		{
			Interval = interval
		};

		_timer.Tick += Tick;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Debounce interval. Can be changed between queues (e.g. longer for large documents).
	/// Takes effect on the next <see cref="Queue"/> restart.
	/// </summary>
	public TimeSpan Interval
	{
		get => _timer.Interval;
		set => _timer.Interval = value;
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		_timer.Stop();
	}

	/// <summary>
	/// Queues <paramref name="value"/> and restarts the debounce timer.
	/// Always restarts even when the value compares equal, so continuous edits
	/// keep delaying delivery until the user is idle.
	/// </summary>
	public void Queue(T value)
	{
		_timer.Stop();
		_value = value;
		_timer.Start();
	}

	private void Tick(object sender, EventArgs e)
	{
		_timer.Stop();
		_execute(_value);
	}

	#endregion
}
