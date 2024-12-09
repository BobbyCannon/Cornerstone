#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone;

public class WeakEventHandler<TEventHandler, TEventArgs>
	where TEventHandler : Delegate
	where TEventArgs : EventArgs
{
	#region Fields

	private readonly List<WeakReference> _listeners;

	#endregion

	#region Constructors

	public WeakEventHandler()
	{
		_listeners = [];
	}

	#endregion

	#region Methods

	public void AddListener(TEventHandler handler)
	{
		_listeners.Add(new WeakReference(handler));
	}

	public void Raise(object sender, TEventArgs args)
	{
		for (var i = _listeners.Count - 1; i >= 0; i--)
		{
			var weakReference = _listeners[i];
			if (weakReference.IsAlive)
			{
				((MulticastDelegate) weakReference.Target)?.DynamicInvoke(sender, args);
			}
			else
			{
				_listeners.RemoveAt(i);
			}
		}
	}

	public void RemoveListener(TEventHandler handler)
	{
		_listeners.RemoveAll(x => !x.IsAlive || (x.Target?.Equals(handler) == true));
	}

	#endregion
}