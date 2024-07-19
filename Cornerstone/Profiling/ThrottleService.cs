#region References

using System;
using System.Threading;
using Cornerstone.Presentation;

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
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public ThrottleService(TimeSpan interval, Action<CancellationToken> action, ITimeProvider timeService = null, IDispatcher dispatcher = null)
		: base(interval, (x, _) => action(x), timeService, dispatcher)
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
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public ThrottleService(TimeSpan interval, Action<CancellationToken, T> action, ITimeProvider timeService = null, IDispatcher dispatcher = null)
		: base(interval, action, timeService, dispatcher)
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
			if (TriggerOnDateTime == DateTime.MinValue)
			{
				return TimeSpan.Zero;
			}

			// Edge case that probably will never happen unless time is being
			// controlled but if trigger on is exactly current time then the delay
			// and the queue is empty then we should consider the trigger processed
			if ((TriggerOnDateTime == CurrentTime) && Queue.IsEmpty)
			{
				return Interval;
			}

			var e = TriggerOnDateTime - CurrentTime;
			if (e <= TimeSpan.Zero)
			{
				return TimeSpan.Zero;
			}

			return e;
		}
	}

	protected override DateTime NextTriggerDate
	{
		get
		{
			if (TriggerOnDateTime == DateTime.MinValue)
			{
				return CurrentTime;
			}

			if ((TriggerOnDateTime > CurrentTime)
				|| (LastTriggerProcessed < TriggerOnDateTime))
			{
				return TriggerOnDateTime;
			}

			var timeSinceLastTrigger = CurrentTime - TriggerOnDateTime;
			if ((timeSinceLastTrigger == TimeSpan.Zero)
				&& (LastTriggerProcessed == TriggerOnDateTime))
			{
				return LastTriggerProcessed + Interval;
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