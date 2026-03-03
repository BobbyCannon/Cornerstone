#region References

using System;
using System.Threading;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// The service to throttle work that supports cancellation.
/// </summary>
public class ThrottleService : ThrottleService<object>
{
	#region Constructors

	/// <summary>
	/// Create an instance of the service for throttling an action.
	/// </summary>
	/// <param name="interval"> The amount of time before the action will trigger. </param>
	/// <param name="action"> The action to throttle. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	public ThrottleService(TimeSpan interval, Action<CancellationToken, bool> action, IDateTimeProvider timeProvider = null)
		: base(interval, (x, _, forced) => action(x, forced), timeProvider)
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
public class ThrottleService<T> : DebounceOrThrottleService<T>
{
	#region Constructors

	/// <summary>
	/// Create an instance of the service for throttling an action.
	/// </summary>
	/// <param name="interval"> The amount of time between each trigger. </param>
	/// <param name="action"> The action to throttle. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	public ThrottleService(TimeSpan interval, Action<CancellationToken, T, bool> action, IDateTimeProvider timeProvider = null)
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

			// Edge case that probably will never happen unless time is being
			// controlled but if trigger on is exactly current time then the delay
			// and the queue is empty then we should consider the trigger processed
			if ((TriggeredOn == CurrentTime) && Queue.IsEmpty)
			{
				return Interval;
			}

			var e = TriggeredOn - CurrentTime;
			if (e <= TimeSpan.Zero)
			{
				return TimeSpan.Zero;
			}

			return e;
		}
	}

	/// <summary>
	/// Calculate the next trigger date.
	/// </summary>
	protected override DateTime NextTriggerDate
	{
		get
		{
			if (TriggeredOn == DateTime.MinValue)
			{
				return CurrentTime;
			}

			if ((TriggeredOn > CurrentTime)
				|| (LastProcessedOn < TriggeredOn))
			{
				return TriggeredOn;
			}

			var timeSinceLastTrigger = CurrentTime - TriggeredOn;
			if ((timeSinceLastTrigger == TimeSpan.Zero)
				&& (LastProcessedOn == TriggeredOn))
			{
				return LastProcessedOn + Interval;
			}

			if (timeSinceLastTrigger < Interval)
			{
				// todo: need test for this
				return CurrentTime + (Interval - timeSinceLastTrigger);
			}

			return CurrentTime;
		}
	}

	#endregion
}