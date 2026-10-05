#region References

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Extensions for dispatcher.
/// </summary>
public static class DispatcherExtensions
{
	#region Methods

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="dispatcher"> The dispatcher to use. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	public static void Dispatch(this IDispatcher dispatcher, Action action, DispatcherPriority priority = default, CancellationToken? cancellationToken = null)
	{
		if (dispatcher.ShouldDispatch())
		{
			dispatcher.Post(action, priority);
			return;
		}

		action();
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="dispatcher"> The dispatcher to use. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	public static T Dispatch<T>(this IDispatcher dispatcher, Func<T> action, DispatcherPriority priority = default, CancellationToken? cancellationToken = null)
	{
		if (!dispatcher.ShouldDispatch())
		{
			return action();
		}

		var tcs = new TaskCompletionSource<T>();
		dispatcher.Post(() => tcs.SetResult(action()), priority);
		return tcs.Task.GetAwaiter().GetResult();
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="dispatcher"> The dispatcher to use. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	public static Task DispatchAsync(this IDispatcher dispatcher, Action action, DispatcherPriority priority = default, CancellationToken? cancellationToken = null)
	{
		if (dispatcher.ShouldDispatch())
		{
			dispatcher.Post(action, priority);
			return Task.CompletedTask;
		}

		action();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="dispatcher"> The dispatcher to use. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	public static Task<T2> DispatchAsync<T2>(this IDispatcher dispatcher, Func<T2> action, DispatcherPriority priority = default, CancellationToken? cancellationToken = null)
	{
		if (!dispatcher.ShouldDispatch())
		{
			return Task.FromResult(action());
		}

		var tcs = new TaskCompletionSource<T2>();
		dispatcher.Post(() => tcs.SetResult(action()), priority);
		return tcs.Task;
	}

	/// <summary>
	/// Returns true if the current context is on the dispatcher thread.
	/// </summary>
	/// <param name="dispatcher"> The dispatcher to use. </param>
	/// <returns> True if on the dispatcher thread otherwise false. </returns>
	public static bool ShouldDispatch(this IDispatcher dispatcher)
	{
		return !dispatcher?.CheckAccess() ?? false;
	}

	#endregion
}
