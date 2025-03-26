#region References

using System;
using System.Timers;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using SystemTimer = System.Timers.Timer;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Average counter for tracking the average a count over a provided time period.
/// </summary>
public class AverageCounter : Bindable, IDisposable
{
	#region Fields

	private readonly Counter _counter;
	private readonly SpeedyList<DateTimeValue<int>> _history;
	private readonly TimeSpan _interval;
	private readonly SystemTimer _timer;
	private readonly IDateTimeProvider _timeProvider;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiate the average service.
	/// </summary>
	/// <param name="interval"> The interval per time slice to track. </param>
	/// <param name="limit"> The maximum amount of timespans to keep track of. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	/// <param name="dispatcher"> The dispatcher. </param>
	public AverageCounter(TimeSpan interval, int limit,
		IDateTimeProvider timeProvider,
		IDispatcher dispatcher
		) : base(dispatcher)
	{
		_interval = interval;
		_timeProvider = timeProvider ?? DateTimeProvider.RealTime;
		_history = new SpeedyList<DateTimeValue<int>>(dispatcher) { Limit = limit };
		_counter = new Counter();
		_timer = new SystemTimer(interval.TotalMilliseconds) { Enabled = false, AutoReset = true };
		_timer.Elapsed += TimerOnElapsed;

		Values = new ReadOnlySpeedyList<DateTimeValue<int>>(_history);
	}

	#endregion

	#region Properties

	public bool IsEnabled
	{
		get => _timer.Enabled;
		set => _timer.Enabled = value;
	}

	public ReadOnlySpeedyList<DateTimeValue<int>> Values { get; }

	#endregion

	#region Methods

	public void Start()
	{
		_timer.Start();
	}

	public void Stop()
	{
		_timer.Stop();
		_timer.Enabled = false;
	}

	/// <summary>
	/// Decrement the counter by a value or default(1) if not provided.
	/// </summary>
	/// <param name="decrease"> The value to be decremented. </param>
	public void Decrement(int decrease = 1)
	{
		_counter.Decrement(decrease);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Increment the counter by a value or default(1) if not provided.
	/// </summary>
	/// <param name="increase"> The value to be incremented. </param>
	public void Increment(int increase = 1)
	{
		_counter.Increment(increase);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		Stop();

		_timer.Stop();
		_timer.Elapsed -= TimerOnElapsed;
		_timer.Dispose();
	}

	private void TimerOnElapsed(object sender, ElapsedEventArgs e)
	{
		_history.Add(new DateTimeValue<int>(_timeProvider.UtcNow, _counter.Value));
		_counter.Reset();
	}

	#endregion
}