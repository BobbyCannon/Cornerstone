#region References

using System;
using System.Diagnostics;
using System.Threading;
using Timer = Cornerstone.Profiling.Timer;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// General utility extensions
/// </summary>
public static class UtilityExtensions
{
	#region Methods

	/// <summary>
	/// Runs action if the test is true.
	/// </summary>
	/// <param name="item"> The item to process. (does nothing) </param>
	/// <param name="test"> The test to validate. </param>
	/// <param name="action"> The action to run if test is true. </param>
	/// <typeparam name="T"> The type the function returns </typeparam>
	public static void IfThen<T>(this T item, Func<T, bool> test, Action<T> action)
	{
		if (test(item))
		{
			action(item);
		}
	}

	/// <summary>
	/// Runs action1 if the test is true or action2 if false.
	/// </summary>
	/// <param name="item"> The item to process. (does nothing) </param>
	/// <param name="test"> The test to validate. </param>
	/// <param name="action1"> The action to run if test is true. </param>
	/// <param name="action2"> The action to run if test is false. </param>
	/// <typeparam name="T"> The type the function returns </typeparam>
	public static void IfThenElse<T>(this T item, Func<T, bool> test, Action<T> action1, Action<T> action2)
	{
		if (test(item))
		{
			action1(item);
		}
		else
		{
			action2(item);
		}
	}

	/// <summary>
	/// Continues to run the action until we hit the timeout. If an exception occurs then delay for the
	/// provided delay time.
	/// </summary>
	/// <typeparam name="T"> The type for this retry. </typeparam>
	/// <param name="action"> The action to attempt to retry. </param>
	/// <param name="timeout"> The timeout to stop retrying. </param>
	/// <param name="delay"> The delay between retries. </param>
	/// <returns> The response from the action. </returns>
	public static T Retry<T>(Func<T> action, int timeout, int delay)
	{
		var watch = Stopwatch.StartNew();

		try
		{
			return action();
		}
		catch (Exception)
		{
			Thread.Sleep(delay);

			var remaining = (int) (timeout - watch.Elapsed.TotalMilliseconds);
			if (remaining <= 0)
			{
				throw;
			}

			return Retry(action, remaining, delay);
		}
	}

	/// <summary>
	/// Continues to run the action until we hit the timeout. If an exception occurs then delay for the
	/// provided delay time.
	/// </summary>
	/// <param name="action"> The action to attempt to retry. </param>
	/// <param name="timeout"> The timeout to attempt the action. This value is in milliseconds. </param>
	/// <param name="delay"> The delay in between actions. This value is in milliseconds. </param>
	/// <returns> The response from the action. </returns>
	public static void Retry(Action action, int timeout, int delay)
	{
		var watch = Stopwatch.StartNew();

		Exception lastEx = null;

		do
		{
			try
			{
				action();
				return;
			}
			catch (Exception ex)
			{
				lastEx = ex;
				Thread.Sleep(delay);

				var remaining = (int) (timeout - watch.Elapsed.TotalMilliseconds);
				if (remaining <= 0)
				{
					throw lastEx;
				}
			}
		} while (lastEx != null);
	}

	/// <summary>
	/// Runs the action until the action returns true or the timeout is reached. Will delay in between actions of the provided
	/// time.
	/// </summary>
	/// <param name="action"> The action to call. </param>
	/// <param name="timeout"> The timeout to attempt the action. This value is in milliseconds. </param>
	/// <param name="delay"> The delay in between actions. This value is in milliseconds. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <returns> Returns true of the call completed successfully or false if it timed out. </returns>
	public static bool WaitUntil(this Func<bool> action, int timeout, int delay, ITimeProvider timeService = null)
	{
		return WaitUntil(action, TimeSpan.FromMilliseconds(timeout), TimeSpan.FromMilliseconds(delay), TimeSpan.MinValue, timeService);
	}

	/// <summary>
	/// Runs the action until the action returns true or the timeout is reached. Will delay in between actions of the provided
	/// time.
	/// </summary>
	/// <param name="action"> The action to call. </param>
	/// <param name="timeout"> The timeout to attempt the action. </param>
	/// <param name="delay"> The delay in between actions. This value is in milliseconds. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <returns> Returns true of the call completed successfully or false if it timed out. </returns>
	public static bool WaitUntil(this Func<bool> action, TimeSpan timeout, int delay, ITimeProvider timeService = null)
	{
		return WaitUntil(action, timeout, TimeSpan.FromMilliseconds(delay), TimeSpan.MinValue, TimeSpan.MaxValue, timeService);
	}

	/// <summary>
	/// Wait for a cancellation or for the value to time out.
	/// </summary>
	/// <param name="action"> The action to call. </param>
	/// <param name="timeout"> The timeout to attempt the action. </param>
	/// <param name="delay"> The delay between checks. </param>
	/// <param name="minimum"> The minimal time to wait. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <returns> True if the wait was completed, false if the wait was cancelled. </returns>
	public static bool WaitUntil(Func<bool> action, TimeSpan timeout, TimeSpan delay, TimeSpan minimum, ITimeProvider timeService = null)
	{
		return WaitUntil(action, timeout, delay, minimum, TimeSpan.MaxValue, timeService);
	}

	/// <summary>
	/// Wait for a cancellation or for the value to time out.
	/// </summary>
	/// <param name="action"> The action to call. </param>
	/// <param name="timeout"> The timeout to attempt the action. </param>
	/// <param name="delay"> The delay between checks. </param>
	/// <param name="minimum"> The minimal time to wait. </param>
	/// <param name="maximum"> The maximum time to wait. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <returns> True if the wait was completed, false if the wait was cancelled. </returns>
	public static bool WaitUntil(Func<bool> action, TimeSpan timeout, TimeSpan delay, TimeSpan minimum, TimeSpan maximum, ITimeProvider timeService = null)
	{
		// Leave here for performance reason, in case cancellation has already been requested
		// Note: this cut 75% of time for existing cancellations
		if (action())
		{
			return true;
		}

		var timer = Timer.StartNewTimer(timeService ?? TimeService.CurrentTime);
		var shouldDelay = delay.Ticks > 0;

		while (((timer.Elapsed < timeout) || (timer.Elapsed < minimum)) && (timer.Elapsed < maximum))
		{
			if (action())
			{
				return true;
			}

			if (shouldDelay)
			{
				Thread.Sleep(delay);
			}
		}

		return false;
	}

	/// <summary>
	/// Wait for a cancellation or for the value to time out.
	/// </summary>
	/// <param name="value"> The value to process in the action. </param>
	/// <param name="action"> The action to call. </param>
	/// <param name="timeout"> The timeout to attempt the action. This value is in milliseconds. </param>
	/// <param name="delay"> The delay between checks. This value is in milliseconds. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <returns> True if the wait was completed, false if the wait was cancelled. </returns>
	public static bool WaitUntil<T>(this T value, Func<bool> action, int timeout, int delay, ITimeProvider timeService = null)
	{
		return WaitUntil(value, _ => action(), TimeSpan.FromMilliseconds(timeout), TimeSpan.FromMilliseconds(delay), TimeSpan.MinValue, timeService);
	}

	/// <summary>
	/// Wait for a cancellation or for the value to time out.
	/// </summary>
	/// <param name="value"> The value to process in the action. </param>
	/// <param name="action"> The action to call. </param>
	/// <param name="timeout"> The timeout to attempt the action. </param>
	/// <param name="delay"> The delay between checks. This value is in milliseconds. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <returns> True if the wait was completed, false if the wait was cancelled. </returns>
	public static bool WaitUntil<T>(this T value, Func<bool> action, TimeSpan timeout, int delay, ITimeProvider timeService = null)
	{
		return WaitUntil(value, _ => action(), timeout, TimeSpan.FromMilliseconds(delay), TimeSpan.MinValue, timeService);
	}

	/// <summary>
	/// Wait for a cancellation or for the value to time out.
	/// </summary>
	/// <param name="value"> The value to process in the action. </param>
	/// <param name="action"> The action to call. </param>
	/// <param name="timeout"> The timeout to attempt the action. </param>
	/// <param name="delay"> The delay between checks. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <returns> True if the wait was completed, false if the wait was cancelled. </returns>
	public static bool WaitUntil<T>(this T value, Func<bool> action, TimeSpan timeout, TimeSpan delay, ITimeProvider timeService = null)
	{
		return WaitUntil(value, _ => action(), timeout, delay, TimeSpan.MinValue, timeService);
	}

	/// <summary>
	/// Wait for a cancellation or for the value to time out.
	/// </summary>
	/// <param name="value"> The value to process in the action. </param>
	/// <param name="action"> The action to call. </param>
	/// <param name="timeout"> The timeout to attempt the action. This value is in milliseconds. </param>
	/// <param name="delay"> The delay between checks. This value is in milliseconds. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <returns> True if the wait was completed, false if the wait was cancelled. </returns>
	public static bool WaitUntil<T>(this T value, Func<T, bool> action, int timeout, int delay, ITimeProvider timeService = null)
	{
		return WaitUntil(() => action(value), TimeSpan.FromMilliseconds(timeout), TimeSpan.FromMilliseconds(delay), TimeSpan.MinValue, timeService);
	}

	/// <summary>
	/// Wait for a cancellation or for the value to time out.
	/// </summary>
	/// <param name="value"> The value to process in the action. </param>
	/// <param name="action"> The action to call. </param>
	/// <param name="timeout"> The timeout to attempt the action. </param>
	/// <param name="delay"> The delay between checks. This value is in milliseconds. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <returns> True if the wait was completed, false if the wait was cancelled. </returns>
	public static bool WaitUntil<T>(this T value, Func<T, bool> action, TimeSpan timeout, int delay, ITimeProvider timeService = null)
	{
		return WaitUntil(() => action(value), timeout, TimeSpan.FromMilliseconds(delay), TimeSpan.MinValue, timeService);
	}

	/// <summary>
	/// Wait for a cancellation or for the value to time out.
	/// </summary>
	/// <param name="value"> The value to process in the action. </param>
	/// <param name="action"> The action to call. </param>
	/// <param name="timeout"> The timeout to attempt the action. </param>
	/// <param name="delay"> The delay between checks. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <returns> True if the wait was completed, false if the wait was cancelled. </returns>
	public static bool WaitUntil<T>(this T value, Func<T, bool> action, TimeSpan timeout, TimeSpan delay, ITimeProvider timeService = null)
	{
		return WaitUntil(() => action(value), timeout, delay, TimeSpan.MinValue, timeService);
	}

	/// <summary>
	/// Wait for a cancellation or for the value to time out.
	/// </summary>
	/// <param name="value"> The value to process in the action. </param>
	/// <param name="action"> The action to call. </param>
	/// <param name="timeout"> The timeout to attempt the action. </param>
	/// <param name="delay"> The delay between checks. </param>
	/// <param name="minimum"> The minimal time to wait. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <returns> True if the wait was completed, false if the wait was cancelled. </returns>
	public static bool WaitUntil<T>(this T value, Func<T, bool> action, TimeSpan timeout, TimeSpan delay, TimeSpan minimum, ITimeProvider timeService = null)
	{
		return WaitUntil(() => action(value), timeout, delay, minimum, timeService);
	}

	#endregion
}