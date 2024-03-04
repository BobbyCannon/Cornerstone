using System;
using System.Threading.Tasks;
using Cornerstone.Presentation;

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for IBindable
/// </summary>
public static class BindableExtensions
{
	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="bindable"> The bindable to dispatch. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public static void Dispatch(this IBindable bindable, Action action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		var dispatcher = bindable.GetDispatcher();
		if (dispatcher?.ShouldDispatch() == true)
		{
			dispatcher.Dispatch(action, priority);
			return;
		}

		action();
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="bindable"> The bindable to dispatch. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public static T2 Dispatch<T2>(this IBindable bindable, Func<T2> action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		var dispatcher = bindable.GetDispatcher();
		return dispatcher?.ShouldDispatch() == true
			? dispatcher.Dispatch(action, priority)
			: action();
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="bindable"> The bindable to dispatch. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public static Task DispatchAsync(this IBindable bindable, Action action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		var dispatcher = bindable.GetDispatcher();
		if (dispatcher?.ShouldDispatch() == true)
		{
			return dispatcher.DispatchAsync(action, priority);
		}

		action();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="bindable"> The bindable to dispatch. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public static Task<T2> DispatchAsync<T2>(this IBindable bindable, Func<T2> action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		var dispatcher = bindable.GetDispatcher();
		if (dispatcher?.ShouldDispatch() == true)
		{
			return dispatcher.DispatchAsync(action, priority);
		}

		var result = action();
		return Task.FromResult(result);
	}

}