#region References

using System;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Internal.WeakEvents;

internal abstract class WeakEventListenerBase<T, TArgs> : IWeakEventListener
	where T : class
{
	#region Fields

	private readonly WeakReference _handler;
	private readonly WeakReference _source;

	#endregion

	#region Constructors

	protected WeakEventListenerBase(T source, EventHandler<TArgs> handler)
	{
		_source = new WeakReference(source);
		_handler = new WeakReference(handler);
	}

	#endregion

	#region Properties

	public Delegate Handler => _handler.IsAlive ? (Delegate) _handler.Target : null;

	public bool IsAlive => _handler.IsAlive && _source.IsAlive;

	public object Source => _source.IsAlive ? _source.Target : null;

	#endregion

	#region Methods

	public void StopListening()
	{
		if (_source.IsAlive)
		{
			StopListening((T) _source.Target);
		}
	}

	protected void HandleEvent(object sender, TArgs e)
	{
		if (IsAlive && _handler.Target is EventHandler<TArgs> handler)
		{
			handler(sender as T, e);
		}
		else
		{
			StopListening();
		}
	}

	protected abstract void StopListening(T source);

	#endregion
}

internal abstract class WeakEventListenerBase<T, TArgs1, TArgs2> : IWeakEventListener
	where T : class
{
	#region Fields

	private readonly WeakReference _handler;
	private readonly WeakReference _source;

	#endregion

	#region Constructors

	protected WeakEventListenerBase(T source, EventHandler<TArgs1, TArgs2> handler)
	{
		_source = new WeakReference(source);
		_handler = new WeakReference(handler);
	}

	#endregion

	#region Properties

	public Delegate Handler => _handler.IsAlive ? (Delegate) _handler.Target : null;

	public bool IsAlive => _handler.IsAlive && _source.IsAlive;

	public object Source => _source.IsAlive ? _source.Target : null;

	#endregion

	#region Methods

	public void StopListening()
	{
		if (_source.IsAlive)
		{
			StopListening((T) _source.Target);
		}
	}

	protected void HandleEvent(object sender, (TArgs1, TArgs2) args)
	{
		if (IsAlive && _handler.Target is EventHandler<TArgs1, TArgs2> handler)
		{
			handler(sender as T, args);
		}
		else
		{
			StopListening();
		}
	}

	protected abstract void StopListening(T source);

	#endregion
}