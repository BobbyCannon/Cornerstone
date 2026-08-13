#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Timer that uses the time service.
/// </summary>
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
[SourceReflection]
public partial class Timer : CornerstoneObject<Timer>, IRequiresDateTimeProvider
{
	#region Fields

	private TimeSpan _elapsed;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of the timer.
	/// </summary>
	public Timer() : this(null)
	{
	}

	/// <summary>
	/// Initializes an instance of the timer.
	/// </summary>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	public Timer(IDateTimeProvider timeProvider) : this(TimeSpan.Zero, timeProvider)
	{
	}

	/// <summary>
	/// Initializes an instance of the timer.
	/// </summary>
	/// <param name="elapsed"> The existing elapsed value. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	public Timer(TimeSpan elapsed, IDateTimeProvider timeProvider)
	{
		_elapsed = elapsed;
		DateTimeProvider = timeProvider ?? Runtime.DateTimeProvider.RealTime;
		StartedOn = DateTime.MinValue;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The time elapsed for the timer.
	/// </summary>
	[UpdateableAction(UpdateableAction.All, 1)]
	public TimeSpan Elapsed => IsLifecycleStarted() ? _elapsed + RunningElapsed() : _elapsed;

	/// <summary>
	/// The time the timer started, if started.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(Elapsed))]
	[UpdateableAction(UpdateableAction.All, 0)]
	public partial DateTime StartedOn { get; private set; }

	/// <summary>
	/// The provider of time.
	/// </summary>
	internal IDateTimeProvider DateTimeProvider { get; private set; }

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
		var oldValue = _elapsed;
		_elapsed = _elapsed.Add(time);
		OnPropertyChanged(nameof(Elapsed), oldValue, _elapsed);
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
		var oldValue = _elapsed;
		_elapsed = elapsed;
		OnPropertyChanged(nameof(Elapsed), oldValue, _elapsed);
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
		var oldValue = _elapsed;
		_elapsed = TimeSpan.Zero;
		OnPropertyChanged(nameof(Elapsed), oldValue, _elapsed);
		StartedOn = dateTime;
	}

	/// <summary>
	/// Starts the timer with a specific time.
	/// </summary>
	/// <param name="dateTime"> The time the timer was started. </param>
	public virtual void Start(DateTime dateTime)
	{
		if (IsLifecycleStarted())
		{
			// should not restart the timer
			return;
		}

		StartedOn = dateTime;

		// Do not trigger the OnPropertyChanged or you risk affecting the timer performance
		//OnPropertyChanged(nameof(Elapsed));

		base.StartLifecycle();
	}

	/// <summary>
	/// Start the timer.
	/// </summary>
	public override void StartLifecycle()
	{
		Start(GetCurrentTime());
	}

	/// <summary>
	/// Creates a timer and starts it running.
	/// </summary>
	/// <returns> The new timer that is currently running. </returns>
	public static Timer StartNewTimer(IDateTimeProvider timeProvider = null)
	{
		var timer = new Timer(timeProvider);
		timer.StartLifecycle();
		return timer;
	}

	/// <summary>
	/// Stops the timer at a specific time.
	/// </summary>
	/// <param name="dateTime"> The time the timer was stopped. </param>
	public TimeSpan Stop(DateTime dateTime)
	{
		if (!IsLifecycleStarted())
		{
			return TimeSpan.Zero;
		}

		var elapsed = dateTime - StartedOn;
		if (elapsed.Ticks > 0)
		{
			var oldValue = _elapsed;
			_elapsed += elapsed;
			OnPropertyChanged(nameof(Elapsed), oldValue, _elapsed);
		}

		StartedOn = DateTime.MinValue;
		return elapsed;
	}

	/// <summary>
	/// Stops the timer.
	/// </summary>
	public override void StopLifecycle()
	{
		Stop(GetCurrentTime());
		base.StopLifecycle();
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
			elapsed = Stop(GetCurrentTime());
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
			StopLifecycle();
		}
	}

	public override string ToString()
	{
		return Elapsed.ToString();
	}

	public void UpdateDateTimeProvider(IDateTimeProvider dateTimeProvider)
	{
		DateTimeProvider = dateTimeProvider;
	}

	/// <summary>
	/// Gets the current time for the timer.
	/// </summary>
	/// <returns> The current time. </returns>
	protected internal virtual DateTime GetCurrentTime()
	{
		return DateTimeProvider.UtcNow;
	}

	/// <summary>
	/// The current running elapsed time.
	/// </summary>
	/// <returns> The running elapsed time. </returns>
	private TimeSpan RunningElapsed()
	{
		var startedOn = StartedOn;
		if (startedOn <= DateTime.MinValue)
		{
			return TimeSpan.Zero;
		}

		var currentTime = GetCurrentTime();
		return currentTime - startedOn;
	}

	#endregion
}