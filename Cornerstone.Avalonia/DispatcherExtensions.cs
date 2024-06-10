#region References

using System;
using Avalonia.Threading;
using CoreDispatcherPriority = Avalonia.Threading.DispatcherPriority;
using DispatcherPriority = Cornerstone.Presentation.DispatcherPriority;

#endregion

namespace Cornerstone.Avalonia;

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
	public static void Dispatch(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
	{
		if (!dispatcher.CheckAccess())
		{
			dispatcher.Post(action, priority.ToDispatcherPriority());
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
	public static T Dispatch<T>(this Dispatcher dispatcher, Func<T> action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		return dispatcher.CheckAccess()
			? dispatcher.Invoke(action, priority.ToDispatcherPriority())
			: action();
	}

	public static CoreDispatcherPriority ToDispatcherPriority(this DispatcherPriority priority)
	{
		return priority switch
		{
			DispatcherPriority.SystemIdle => CoreDispatcherPriority.ContextIdle,
			DispatcherPriority.ApplicationIdle => CoreDispatcherPriority.ContextIdle,
			DispatcherPriority.ContextIdle => CoreDispatcherPriority.ContextIdle,
			DispatcherPriority.Background => CoreDispatcherPriority.Background,
			DispatcherPriority.Normal => CoreDispatcherPriority.Normal,
			_ => CoreDispatcherPriority.Normal
		};
	}

	#endregion
}