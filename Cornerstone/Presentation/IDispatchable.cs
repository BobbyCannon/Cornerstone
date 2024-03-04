#region References

using System;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a dispatchable object.
/// </summary>
public interface IDispatchable
{
	#region Methods

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public void Dispatch(Action action, DispatcherPriority priority = DispatcherPriority.Normal);

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public T Dispatch<T>(Func<T> action, DispatcherPriority priority = DispatcherPriority.Normal);

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public Task DispatchAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal);

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public Task<T2> DispatchAsync<T2>(Func<T2> action, DispatcherPriority priority = DispatcherPriority.Normal);

	#endregion
}