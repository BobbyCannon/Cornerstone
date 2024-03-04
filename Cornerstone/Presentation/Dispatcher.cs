#region References

using System;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Presentation;

/// <inheritdoc />
public abstract class Dispatcher : IDispatcher
{
	/// <summary>
	/// Initialize the dispatcher.
	/// </summary>
	protected Dispatcher()
	{
		// Enable dispatchers by default.
		IsEnabled = true;
	}

	#region Properties

	/// <inheritdoc />
	public abstract bool IsDispatcherThread { get; }

	/// <inheritdoc />
	public bool IsEnabled { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Dispatch(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		if (this.ShouldDispatch())
		{
			ExecuteOnDispatcher(action, priority);
			return;
		}

		action();
	}

	/// <inheritdoc />
	public T Dispatch<T>(Func<T> action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		return this.ShouldDispatch()
			? ExecuteOnDispatcher(action, priority)
			: action();
	}

	/// <inheritdoc />
	public Task DispatchAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		if (this.ShouldDispatch())
		{
			return ExecuteOnDispatcherAsync(action, priority);
		}

		action();
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<T> DispatchAsync<T>(Func<T> action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		return this.ShouldDispatch()
			? ExecuteOnDispatcherAsync(action, priority)
			: Task.FromResult(action());
	}

	/// <summary>
	/// Execute the action on the dispatcher.
	/// </summary>
	/// <param name="action"> The action to execute. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	protected abstract void ExecuteOnDispatcher(Action action, DispatcherPriority priority);

	/// <summary>
	/// Execute the action on the dispatcher.
	/// </summary>
	/// <param name="action"> The action to execute. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	protected abstract T ExecuteOnDispatcher<T>(Func<T> action, DispatcherPriority priority);

	/// <summary>
	/// Execute the action on the dispatcher.
	/// </summary>
	/// <param name="action"> The action to execute. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	protected abstract Task ExecuteOnDispatcherAsync(Action action, DispatcherPriority priority);

	/// <summary>
	/// Execute the action on the dispatcher.
	/// </summary>
	/// <param name="action"> The action to execute. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	protected abstract Task<T> ExecuteOnDispatcherAsync<T>(Func<T> action, DispatcherPriority priority);

	#endregion
}

/// <summary>
/// Represents a dispatcher to help with handling dispatcher thread access.
/// </summary>
public interface IDispatcher : IDispatchable
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
}