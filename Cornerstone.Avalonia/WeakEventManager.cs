#region References

using System;
using Avalonia;
using Cornerstone.Internal.WeakEvents;

#endregion

namespace Cornerstone.Avalonia;

/// <summary>
/// Provides methods for registering and unregistering event handlers that
/// don't cause memory "leaks" when the lifetime of the listener is longer
/// than the lifetime of the object being listened to.
/// </summary>
public static class WeakEventManagerExtensions
{
	#region Methods

	/// <summary>
	/// Registers the given delegate as a handler for the INotifyPropertyChanged.PropertyChanged event
	/// </summary>
	public static void AddPropertyChanged<T>(this WeakEventManager manager, T source, EventHandler<AvaloniaPropertyChangedEventArgs> handler)
		where T : AvaloniaObject
	{
		manager._listeners.Add(new AvaloniaPropertyChangedWeakEventListener<T>(source, handler), handler);
	}

	#endregion

	#region Classes

	private class AvaloniaPropertyChangedWeakEventListener<T> : WeakEventListenerBase<T, AvaloniaPropertyChangedEventArgs>
		where T : AvaloniaObject
	{
		#region Constructors

		public AvaloniaPropertyChangedWeakEventListener(T source, EventHandler<AvaloniaPropertyChangedEventArgs> handler)
			: base(source, handler)
		{
			source.PropertyChanged += HandleEvent;
		}

		#endregion

		#region Methods

		protected override void StopListening(T source)
		{
			source.PropertyChanged -= HandleEvent;
		}

		#endregion
	}

	#endregion
}