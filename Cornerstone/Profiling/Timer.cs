﻿#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Timer that uses the time service.
/// </summary>
public class Timer : Bindable
{
	#region Fields

	private TimeSpan _elapsed;
	private DateTime _startedOn;
	private readonly IDateTimeProvider _timeService;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of the timer.
	/// </summary>
	public Timer() : this(null, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the timer.
	/// </summary>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	public Timer(IDateTimeProvider timeService) : this(timeService, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the timer.
	/// </summary>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public Timer(IDateTimeProvider timeService, IDispatcher dispatcher) : base(dispatcher)
	{
		_elapsed = TimeSpan.Zero;
		_timeService = timeService ?? TimeService.RealTime;
		_startedOn = DateTime.MinValue;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The time elapsed for the timer.
	/// </summary>
	public TimeSpan Elapsed => IsRunning ? _elapsed + RunningElapsed() : _elapsed;

	/// <summary>
	/// Indicates the timer is running or not.
	/// </summary>
	public bool IsRunning => _startedOn > DateTime.MinValue;

	#endregion

	#region Methods

	/// <summary>
	/// Adds the time value to this timer.
	/// </summary>
	/// <param name="time"> The time to be added. </param>
	public void Add(TimeSpan time)
	{
		_elapsed = _elapsed.Add(time);

		NotifyOfPropertyChanged(nameof(Elapsed));
	}

	/// <summary>
	/// Create a new timer and processes provided function.
	/// </summary>
	/// <param name="function"> The action to be timed. </param>
	/// <returns> The value return from the function and the new timer. </returns>
	public static Timer Create(Action function)
	{
		var timer = new Timer();
		timer.Time(function);
		return timer;
	}

	/// <summary>
	/// Create a new timer and processes provided function.
	/// </summary>
	/// <typeparam name="T"> The type of the response from the function. </typeparam>
	/// <param name="function"> The action to be timed. </param>
	/// <returns> The value return from the function and the new timer. </returns>
	public static (T result, Timer timer) Create<T>(Func<T> function)
	{
		var timer = new Timer();
		var response = timer.Time(function);
		return (response, timer);
	}

	/// <summary>
	/// Reset the timer.
	/// </summary>
	public virtual void Reset()
	{
		Reset(TimeSpan.Zero);
	}

	/// <summary>
	/// Reset the time while provided an elapsed timer.
	/// </summary>
	/// <param name="elapsed"> The value to set elapsed to. </param>
	public virtual void Reset(TimeSpan elapsed)
	{
		_elapsed = elapsed;
		_startedOn = DateTime.MinValue;

		NotifyOfPropertyChanged(nameof(Elapsed));
	}

	/// <summary>
	/// Restarts the timer.
	/// </summary>
	public virtual void Restart()
	{
		Restart(GetCurrentTime());
	}

	/// <summary>
	/// Restarts the timer with a specific time. The elapsed time will be reset.
	/// </summary>
	/// <param name="dateTime"> The time the timer was started. </param>
	public virtual void Restart(DateTime dateTime)
	{
		_elapsed = TimeSpan.Zero;
		_startedOn = dateTime;

		NotifyOfPropertyChanged(nameof(Elapsed));
	}

	/// <summary>
	/// Start the timer.
	/// </summary>
	public virtual void Start()
	{
		Start(GetCurrentTime());
	}

	/// <summary>
	/// Starts the timer with a specific time.
	/// </summary>
	/// <param name="dateTime"> The time the timer was started. </param>
	public virtual void Start(DateTime dateTime)
	{
		if (IsRunning)
		{
			// should not restart the timer
			return;
		}

		_startedOn = dateTime;

		// Do not trigger the OnPropertyChanged or you risk affecting the timer performance
		//OnPropertyChanged(nameof(Elapsed));
	}

	/// <summary>
	/// Creates a timer and starts it running.
	/// </summary>
	/// <returns> The new timer that is currently running. </returns>
	public static Timer StartNewTimer(IDateTimeProvider timeService = null)
	{
		var timer = new Timer(timeService, null);
		timer.Start();
		return timer;
	}

	/// <summary>
	/// Stops the timer.
	/// </summary>
	public virtual TimeSpan Stop()
	{
		return Stop(GetCurrentTime());
	}

	/// <summary>
	/// Stops the timer at a specific time.
	/// </summary>
	/// <param name="dateTime"> The time the timer was stopped. </param>
	public virtual TimeSpan Stop(DateTime dateTime)
	{
		if (!IsRunning)
		{
			return TimeSpan.Zero;
		}

		var elapsed = dateTime - _startedOn;
		if (elapsed.Ticks > 0)
		{
			_elapsed += elapsed;
		}

		_startedOn = DateTime.MinValue;

		NotifyOfPropertyChanged(nameof(Elapsed));
		return elapsed;
	}

	/// <summary>
	/// Start the timer, performs the action, then stops the timer.
	/// </summary>
	/// <param name="action"> The action to be timed. </param>
	public TimeSpan Time(Action action)
	{
		TimeSpan elapsed;

		try
		{
			// Just set the field directly for performance reasons
			_startedOn = GetCurrentTime();
			action();
		}
		finally
		{
			elapsed = Stop();
		}

		return elapsed;
	}

	/// <summary>
	/// Start the timer, performs the function, then stops the timer, then returns the value from the function.
	/// </summary>
	/// <param name="function"> The action to be timed. </param>
	/// <returns> The value return from the function. </returns>
	public T Time<T>(Func<T> function)
	{
		try
		{
			// Just set the field directly for performance reasons
			_startedOn = GetCurrentTime();
			return function();
		}
		finally
		{
			Stop();
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return Elapsed.ToString();
	}

	/// <summary>
	/// Gets the current time for the timer.
	/// </summary>
	/// <returns> The current time. </returns>
	protected virtual DateTime GetCurrentTime()
	{
		return _timeService.UtcNow;
	}

	/// <summary>
	/// The current running elapsed time.
	/// </summary>
	/// <returns> The running elapsed time. </returns>
	private TimeSpan RunningElapsed()
	{
		var currentTime = GetCurrentTime();
		return currentTime - _startedOn;
	}

	#endregion
}