#region References

using System;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Average timer for tracking the average processing time of work.
/// </summary>
public class AverageTimer : Bindable
{
	#region Fields

	private readonly SpeedyList<long> _collection;
	private readonly Timer _timer;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiate the average service.
	/// </summary>
	public AverageTimer() : this(0, null, null)
	{
	}

	/// <summary>
	/// Instantiate the average service.
	/// </summary>
	/// <param name="limit"> The maximum amount of values to average. </param>
	public AverageTimer(int limit) : this(limit, null, null)
	{
	}

	/// <summary>
	/// Instantiate the average service.
	/// </summary>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	public AverageTimer(IDateTimeProvider timeProvider) : this(0, timeProvider, null)
	{
	}

	/// <summary>
	/// Instantiate the average service.
	/// </summary>
	/// <param name="dispatcher"> The dispatcher. </param>
	public AverageTimer(IDispatcher dispatcher) : this(0, null, dispatcher)
	{
	}

	/// <summary>
	/// Instantiate the average service.
	/// </summary>
	/// <param name="limit"> The maximum amount of values to average. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	/// <param name="dispatcher"> The dispatcher. </param>
	public AverageTimer(int limit, IDateTimeProvider timeProvider, IDispatcher dispatcher) : base(dispatcher)
	{
		_collection = new SpeedyList<long>(dispatcher) { Limit = limit };
		_timer = new Timer(timeProvider, dispatcher);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Returns the Average value as TimeSpan. This expects the Average values to be "Ticks".
	/// </summary>
	public TimeSpan Average { get; private set; }

	/// <summary>
	/// Number of times this timer has been called.
	/// </summary>
	public int Count { get; private set; }

	/// <summary>
	/// The amount of time that has elapsed.
	/// </summary>
	public TimeSpan Elapsed => _timer?.Elapsed ?? TimeSpan.Zero;

	/// <summary>
	/// Indicates if the timer is running;
	/// </summary>
	public bool IsRunning => _timer?.IsRunning ?? false;

	/// <summary>
	/// Number of samples currently being averaged.
	/// </summary>
	public int Samples { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Cancel the timer.
	/// </summary>
	public void Cancel()
	{
		_timer.Stop();

		OnPropertyChanged(nameof(Elapsed));
		OnPropertyChanged(nameof(IsRunning));
	}

	/// <summary>
	/// Reset the average timer.
	/// </summary>
	public void Reset()
	{
		_collection.Clear();
		_timer.Reset();

		Average = TimeSpan.Zero;
		Samples = 0;
		Count = 0;

		OnPropertyChanged(nameof(Elapsed));
		OnPropertyChanged(nameof(IsRunning));
	}

	/// <summary>
	/// Start the timer.
	/// </summary>
	public void Start()
	{
		Start(_timer.GetCurrentTime());
	}

	/// <summary>
	/// Start the timer.
	/// </summary>
	public void Start(DateTime startedOn)
	{
		_timer.Reset();
		_timer.Restart(startedOn);

		OnPropertyChanged(nameof(Elapsed));
		OnPropertyChanged(nameof(IsRunning));
	}

	/// <summary>
	/// Stop the timer then update the average.
	/// </summary>
	public void Stop()
	{
		Stop(_timer.GetCurrentTime());
	}

	/// <summary>
	/// Stop the timer then update the average.
	/// </summary>
	public void Stop(DateTime stoppedOn)
	{
		try
		{
			if (!IsRunning)
			{
				// The timer is not running so do not mess up the average
				return;
			}

			_timer.Stop(stoppedOn);

			if (_collection.Limit == 0)
			{
				Average = Count <= 0 ? _timer.Elapsed : TimeSpan.FromTicks((Average.Ticks + Elapsed.Ticks) / 2);
				Count++;
				return;
			}

			_collection.Add(_timer.Elapsed.Ticks);

			Samples = _collection.Count;
			Average = new TimeSpan((long) _collection.Average());
			Count++;
		}
		finally
		{
			OnPropertyChanged(nameof(Elapsed));
			OnPropertyChanged(nameof(IsRunning));
		}
	}

	/// <summary>
	/// Start the timer, performs the action, then stops the timer.
	/// </summary>
	/// <param name="action"> The action to be timed. </param>
	public void Time(Action action)
	{
		try
		{
			Start();
			action();
		}
		finally
		{
			Stop();
		}
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
			Start();
			return function();
		}
		finally
		{
			Stop();
		}
	}

	/// <summary>
	/// Update the AverageTimer with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	public virtual bool UpdateWith(AverageTimer update)
	{
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	/// <summary>
	/// Update the AverageTimer with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(AverageTimer update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			Average = update.Average;
			Count = update.Count;
			Samples = update.Samples;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Average)), x => x.Average = update.Average);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Count)), x => x.Count = update.Count);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Samples)), x => x.Samples = update.Samples);
		}

		_collection.Reconcile(update._collection);
		_timer.UpdateWith(update._timer);

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			AverageTimer value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}