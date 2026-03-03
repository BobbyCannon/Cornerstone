#region References

using System;
using System.Threading;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// The service to throttle work that supports cancellation.
/// </summary>
public class DebounceService : DebounceService<object>
{
	#region Constructors

	/// <summary>
	/// Create an instance of the service for debouncing an action.
	/// </summary>
	/// <param name="interval"> The amount of time before the action will trigger. </param>
	/// <param name="action"> The action to debounce. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	public DebounceService(TimeSpan interval, Action<CancellationToken, bool> action, IDateTimeProvider timeProvider = null)
		: base(interval, (x, _, f) => action(x, f), timeProvider)
	{
	}

	#endregion

	#region Methods

	/// <summary>
	/// Trigger the service. Will be trigger after the timespan.
	/// </summary>
	/// <param name="force"> An optional flag to immediately trigger if true. Defaults to false. </param>
	public void Trigger(bool force = false)
	{
		Trigger(null, force);
	}

	#endregion
}

/// <summary>
/// The service to throttle work that supports cancellation.
/// </summary>
public class DebounceService<T> : DebounceOrThrottleService<T>
{
	#region Constructors

	/// <summary>
	/// Create an instance of the service for throttling an action.
	/// </summary>
	/// <param name="interval"> The amount of time between each trigger. </param>
	/// <param name="action"> The action to throttle. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	public DebounceService(TimeSpan interval, Action<CancellationToken, T, bool> action, IDateTimeProvider timeProvider = null)
		: base(interval, action, timeProvider)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// The timespan until next trigger
	/// </summary>
	public override TimeSpan TimeToNextTrigger
	{
		get
		{
			if (TriggeredOn == DateTime.MinValue)
			{
				return Interval;
			}

			var elapsed = CurrentTime - TriggeredOn;
			if (elapsed < TimeSpan.Zero)
			{
				return TimeSpan.Zero;
			}

			return Interval - elapsed;
		}
	}

	protected override DateTime NextTriggerDate => CurrentTime;

	#endregion
}