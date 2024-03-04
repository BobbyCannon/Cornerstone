#region References

using System;
using System.Windows;
using Dispatcher = System.Windows.Threading.Dispatcher;
using DispatcherPriority = System.Windows.Threading.DispatcherPriority;

#endregion

namespace Cornerstone.Wpf.Extensions;

/// <summary>
/// Extensions for dispatcher.
/// </summary>
public static class DispatcherExtensions
{
	#region Methods

	/// <summary>
	/// Begin invoke on the element.
	/// </summary>
	public static void BeginInvoke<T>(this T element, Action<T> action, DispatcherPriority priority = DispatcherPriority.Normal)
		where T : UIElement
	{
		element.Dispatcher.BeginInvoke(priority, new Action(() => action(element)));
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="dispatcher"> The dispatcher to use. </param>
	/// <param name="action"> The action to be executed. </param>
	public static T Dispatch<T>(this Dispatcher dispatcher, Func<T> action)
	{
		return dispatcher.ShouldDispatch()
			? dispatcher.Invoke(action)
			: action();
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="element"> The element dispatcher to use. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public static void Dispatch(this FrameworkElement element, Action action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		if (element.Dispatcher.ShouldDispatch())
		{
			element.Dispatcher.Invoke(action, priority);
			return;
		}

		action();
	}

	/// <summary>
	/// Run an action on the dispatching thread if available and required.
	/// </summary>
	/// <param name="element"> The element dispatcher to use. </param>
	/// <param name="action"> The action to be executed. </param>
	/// <param name="priority"> An optional priority for the action. </param>
	public static T Dispatch<T>(this FrameworkElement element, Func<T> action, DispatcherPriority priority = DispatcherPriority.Normal)
	{
		return element.Dispatcher.ShouldDispatch()
			? element.Dispatcher.Invoke(action, priority)
			: action();
	}

	/// <summary>
	/// Returns true if the current context is on the dispatcher thread.
	/// </summary>
	/// <param name="dispatcher"> The dispatcher to use. </param>
	/// <returns> True if on the dispatcher thread otherwise false. </returns>
	public static bool ShouldDispatch(this Dispatcher dispatcher)
	{
		return !dispatcher.HasShutdownStarted
			&& !dispatcher.HasShutdownFinished
			&& !dispatcher.CheckAccess();
	}

	#endregion
}