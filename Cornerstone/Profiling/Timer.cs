#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Timer that uses the time service.
/// </summary>
public class Timer : Bindable<Timer>
{
	#region Fields

	private TimeSpan _elapsed;

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
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	public Timer(IDateTimeProvider timeProvider) : this(timeProvider, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the timer.
	/// </summary>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public Timer(IDateTimeProvider timeProvider, IDispatcher dispatcher) : base(dispatcher)
	{
		_elapsed = TimeSpan.Zero;
		TimeProvider = timeProvider ?? DateTimeProvider.RealTime;
		StartedOn = DateTime.MinValue;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The time elapsed for the timer.
	/// </summary>
	public TimeSpan Elapsed
	{
		get => IsRunning ? _elapsed + RunningElapsed() : _elapsed;
		set => _elapsed = value;
	}

	/// <summary>
	/// Indicates the timer is running or not.
	/// </summary>
	public bool IsRunning => StartedOn > DateTime.MinValue;

	/// <summary>
	/// The time the timer started, if started.
	/// </summary>
	public DateTime StartedOn { get; private set; }

	/// <summary>
	/// The provider of time.
	/// </summary>
	internal IDateTimeProvider TimeProvider { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Adds the average timer elapsed value to this timer.
	/// </summary>
	/// <param name="timer"> The timer to be added. </param>
	public void Add(AverageTimer timer)
	{
		Add(timer.Elapsed);
	}

	/// <summary>
	/// Adds the average timer elapsed value to this timer.
	/// </summary>
	/// <param name="timer"> The timer to be added. </param>
	public void Add(Timer timer)
	{
		Add(timer.Elapsed);
	}

	/// <summary>
	/// Adds the time value to this timer.
	/// </summary>
	/// <param name="time"> The time to be added. </param>
	public void Add(TimeSpan time)
	{
		Elapsed = _elapsed.Add(time);
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
	/// <param name="timer"> The value to set elapsed to. </param>
	public void Reset(Timer timer)
	{
		Reset(timer.Elapsed);
	}

	/// <summary>
	/// Reset the time while provided an elapsed timer.
	/// </summary>
	/// <param name="elapsed"> The value to set elapsed to. </param>
	public virtual void Reset(TimeSpan elapsed)
	{
		Elapsed = elapsed;
		StartedOn = DateTime.MinValue;
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
		Elapsed = TimeSpan.Zero;
		StartedOn = dateTime;
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

		StartedOn = dateTime;

		// Do not trigger the OnPropertyChanged or you risk affecting the timer performance
		//OnPropertyChanged(nameof(Elapsed));
	}

	/// <summary>
	/// Creates a timer and starts it running.
	/// </summary>
	/// <returns> The new timer that is currently running. </returns>
	public static Timer StartNewTimer(IDateTimeProvider timeProvider = null, IDispatcher dispatcher = null)
	{
		var timer = new Timer(timeProvider, dispatcher);
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

		var elapsed = dateTime - StartedOn;
		if (elapsed.Ticks > 0)
		{
			_elapsed += elapsed;
		}

		StartedOn = DateTime.MinValue;

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
			StartedOn = GetCurrentTime();
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
			StartedOn = GetCurrentTime();
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

	/// <inheritdoc />
	public override bool UpdateWith(Timer update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			_elapsed = update._elapsed;
			StartedOn = update.StartedOn;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Elapsed)), x => x._elapsed = update._elapsed);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(StartedOn)), x => x.StartedOn = update.StartedOn);
		}

		return true;
	}

	/// <summary>
	/// Gets the current time for the timer.
	/// </summary>
	/// <returns> The current time. </returns>
	protected internal virtual DateTime GetCurrentTime()
	{
		return TimeProvider.UtcNow;
	}

	/// <summary>
	/// The current running elapsed time.
	/// </summary>
	/// <returns> The running elapsed time. </returns>
	private TimeSpan RunningElapsed()
	{
		var currentTime = GetCurrentTime();
		return currentTime - StartedOn;
	}

	#endregion
}