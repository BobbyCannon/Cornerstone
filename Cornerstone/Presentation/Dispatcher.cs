#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation;

[SourceReflection]
public abstract class Dispatcher : IDispatcher
{
	#region Constructors

	/// <summary>
	/// Initialize the dispatcher.
	/// </summary>
	protected Dispatcher()
	{
		// Enable dispatchers by default.
		IsEnabled = true;
	}

	#endregion

	#region Properties

	public abstract bool IsDispatcherThread { get; }

	public bool IsEnabled { get; set; }

	#endregion

	#region Methods

	public void Dispatch(Action action,
		DispatcherPriority priority = DispatcherPriority.Normal,
		CancellationToken? cancellationToken = null)
	{
		if (this.ShouldDispatch())
		{
			ExecuteOnDispatcher(action, priority, cancellationToken ?? CancellationToken.None);
			return;
		}

		action();
	}

	public T Dispatch<T>(Func<T> action,
		DispatcherPriority priority = DispatcherPriority.Normal,
		CancellationToken? cancellationToken = null)
	{
		return this.ShouldDispatch()
			? ExecuteOnDispatcher(action, priority, cancellationToken ?? CancellationToken.None)
			: action();
	}

	public Task DispatchAsync(Action action,
		DispatcherPriority priority = DispatcherPriority.Normal,
		CancellationToken? cancellationToken = null)
	{
		if (this.ShouldDispatch())
		{
			return ExecuteOnDispatcherAsync(action, priority, cancellationToken ?? CancellationToken.None);
		}

		action();
		return Task.CompletedTask;
	}

	public Task<T> DispatchAsync<T>(Func<T> action,
		DispatcherPriority priority = DispatcherPriority.Normal,
		CancellationToken? cancellationToken = null)
	{
		return this.ShouldDispatch()
			? ExecuteOnDispatcherAsync(action, priority, cancellationToken ?? CancellationToken.None)
			: Task.FromResult(action());
	}

	/// <summary>
	/// Execute the action on the dispatcher.
	/// </summary>
	/// <param name="action"> The action to execute. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	protected abstract void ExecuteOnDispatcher(Action action, DispatcherPriority priority, CancellationToken cancellationToken);

	/// <summary>
	/// Execute the action on the dispatcher.
	/// </summary>
	/// <param name="action"> The action to execute. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	protected abstract T ExecuteOnDispatcher<T>(Func<T> action, DispatcherPriority priority, CancellationToken cancellationToken);

	/// <summary>
	/// Execute the action on the dispatcher.
	/// </summary>
	/// <param name="action"> The action to execute. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	protected abstract Task ExecuteOnDispatcherAsync(Action action, DispatcherPriority priority, CancellationToken cancellationToken);

	/// <summary>
	/// Execute the action on the dispatcher.
	/// </summary>
	/// <param name="action"> The action to execute. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	protected abstract Task<T> ExecuteOnDispatcherAsync<T>(Func<T> action, DispatcherPriority priority, CancellationToken cancellationToken);

	#endregion
}

/// <summary>
/// Represents a dispatcher to help with handling dispatcher thread access.
/// </summary>
public interface IDispatcher
{
	#region Properties

	/// <summary>
	/// Returns true if currently executing on the dispatcher thread.
	/// </summary>
	bool IsDispatcherThread { get; }

	/// <summary>
	/// Returns true if dispatcher thread is enabled.
	/// </summary>
	bool IsEnabled { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	public void Dispatch(Action action, DispatcherPriority priority = DispatcherPriority.Normal, CancellationToken? cancellationToken = null);

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	public T Dispatch<T>(Func<T> action, DispatcherPriority priority = DispatcherPriority.Normal, CancellationToken? cancellationToken = null);

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	public Task DispatchAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal, CancellationToken? cancellationToken = null);

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the operation. </param>
	public Task<T2> DispatchAsync<T2>(Func<T2> action, DispatcherPriority priority = DispatcherPriority.Normal, CancellationToken? cancellationToken = null);

	#endregion
}