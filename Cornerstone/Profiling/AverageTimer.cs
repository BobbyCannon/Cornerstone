#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Average timer for tracking the average processing time of work.
/// </summary>
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
[SourceReflection]
public partial class AverageTimer : CornerstoneObject<AverageTimer>
{
	#region Constants

	private const int Default = 10;
	private const int Maximum = 10000;

	#endregion

	#region Fields

	private readonly long[] _buffer;
	private int _index;
	private long _sum;
	private readonly Timer _timer;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiate the average service.
	/// </summary>
	public AverageTimer() : this(Default, null)
	{
	}

	/// <summary>
	/// Instantiate the average service.
	/// </summary>
	/// <param name="limit"> The maximum amount of values to average. </param>
	public AverageTimer(int limit) : this(limit, null)
	{
	}

	/// <summary>
	/// Instantiate the average service.
	/// </summary>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	public AverageTimer(IDateTimeProvider timeProvider) : this(Default, timeProvider)
	{
	}

	/// <summary>
	/// Instantiate the average service.
	/// </summary>
	/// <param name="limit"> The maximum amount of values to average. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	public AverageTimer(int limit, IDateTimeProvider timeProvider)
	{
		_buffer = new long[limit.EnsureRange(2, Maximum)];
		_timer = new Timer(timeProvider);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Returns the Average value as TimeSpan. This expects the Average values to be "Ticks".
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial TimeSpan Average { get; private set; }

	/// <summary>
	/// Number of times this timer has been called.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Count { get; private set; }

	/// <summary>
	/// The amount of time that has elapsed.
	/// </summary>
	public TimeSpan Elapsed => _timer?.Elapsed ?? TimeSpan.Zero;

	/// <summary>
	/// Indicates if the timer is running;
	/// </summary>
	public bool IsRunning => _timer?.IsLifecycleStarted() ?? false;

	/// <summary>
	/// Number of samples currently being averaged.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Samples { get; private set; }

	/// <summary>
	/// An optional tag field to use for custom tracking purposes.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Tag { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Cancel the timer.
	/// </summary>
	public void Cancel()
	{
		_timer.StopLifecycle();

		NotifyComputedPropertyChanged(nameof(Elapsed));
		NotifyComputedPropertyChanged(nameof(IsRunning));
	}

	public static TimeSpan PerformAverage(Action action, int runs, int discardCount = 1)
	{
		if (runs < 3)
		{
			throw new ArgumentException("Runs must be at least 3");
		}

		var times = new List<long>();
		var sw = new Stopwatch();

		for (var i = 0; i < runs; i++)
		{
			sw.Restart();
			action();
			sw.Stop();
			times.Add(sw.Elapsed.Ticks);
		}

		times.Sort();
		var trimmed = times.Skip(discardCount).Take(times.Count - (2 * discardCount));
		return new TimeSpan((long) trimmed.Average());
	}

	/// <summary>
	/// Reset the average timer.
	/// </summary>
	public void Reset()
	{
		_index = 0;
		_sum = 0;
		_timer.Reset();

		Average = TimeSpan.Zero;
		Samples = 0;
		Count = 0;

		NotifyComputedPropertyChanged(nameof(Elapsed));
		NotifyComputedPropertyChanged(nameof(IsRunning));
	}

	/// <summary>
	/// Start the timer.
	/// </summary>
	/// <param name="startedOn"> The time to start the timer from. </param>
	public void Start(DateTime startedOn)
	{
		_timer.Reset();
		_timer.Restart(startedOn);

		NotifyComputedPropertyChanged(nameof(Elapsed));
		NotifyComputedPropertyChanged(nameof(IsRunning));
	}

	/// <summary>
	/// Start the timer.
	/// </summary>
	public override void StartLifecycle()
	{
		Start(_timer.GetCurrentTime());
	}

	/// <summary>
	/// Stop the timer then update the average.
	/// </summary>
	/// <param name="stoppedOn"> The time to stop the timer at. </param>
	public void Stop(DateTime stoppedOn)
	{
		try
		{
			if (!IsRunning)
			{
				return;
			}

			_timer.Stop(stoppedOn);

			var ticks = _timer.Elapsed.Ticks;
			var limit = _buffer.Length;

			if (_index >= limit)
			{
				_sum -= _buffer[_index % limit];
			}

			_buffer[_index % limit] = ticks;
			_sum += ticks;
			_index++;

			Samples = Math.Min(_index, limit);
			Average = new TimeSpan(_sum / Samples);
			Count++;
		}
		finally
		{
			NotifyComputedPropertyChanged(nameof(Elapsed));
			NotifyComputedPropertyChanged(nameof(IsRunning));
		}
	}

	/// <summary>
	/// Stop the timer then update the average.
	/// </summary>
	public override void StopLifecycle()
	{
		Stop(_timer.GetCurrentTime());
	}

	/// <summary>
	/// Start the timer, performs the action, then stops the timer.
	/// </summary>
	/// <param name="action"> The action to be timed. </param>
	public void Time(Action action)
	{
		try
		{
			StartLifecycle();
			action();
		}
		finally
		{
			StopLifecycle();
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
			StartLifecycle();
			return function();
		}
		finally
		{
			StopLifecycle();
		}
	}

	#endregion
}