#region References

using System;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Extensions for dispatchable.
/// </summary>
public static class DispatchableExtensions
{
	#region Methods

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="dispatchable"> The dispatchable to use. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public static void Dispatch(this IDispatchable dispatchable, Action action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		var dispatcher = dispatchable.GetDispatcher();
		if (dispatcher.ShouldDispatch())
		{
			dispatcher.Dispatch(action, priority);
			return;
		}

		action();
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="dispatchable"> The dispatchable to use. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public static T Dispatch<T>(this IDispatchable dispatchable, Func<T> action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		var dispatcher = dispatchable.GetDispatcher();
		return dispatcher.ShouldDispatch()
			? dispatcher.Dispatch(action, priority)
			: action();
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="dispatchable"> The dispatchable to use. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public static Task DispatchAsync(this IDispatchable dispatchable, Action action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		var dispatcher = dispatchable.GetDispatcher();
		if (dispatcher.ShouldDispatch())
		{
			return dispatcher.DispatchAsync(action, priority);
		}

		action();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="dispatchable"> The dispatchable to use. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public static Task<T2> DispatchAsync<T2>(this IDispatchable dispatchable, Func<T2> action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		var dispatcher = dispatchable.GetDispatcher();
		if (dispatcher.ShouldDispatch())
		{
			return dispatcher.DispatchAsync(action, priority);
		}

		var result = action();
		return Task.FromResult(result);
	}

	/// <summary>
	/// Returns true if the current context is on the dispatchable thread.
	/// </summary>
	/// <param name="dispatchable"> The dispatchable to use. </param>
	/// <returns> True if on the dispatchable thread otherwise false. </returns>
	public static bool ShouldDispatch(this IDispatchable dispatchable)
	{
		var dispatcher = dispatchable.GetDispatcher();
		return dispatcher is { IsEnabled: true, IsDispatcherThread: false };
	}

	#endregion
}