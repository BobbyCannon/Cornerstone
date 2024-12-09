#region References

using System;

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
		//_source = new WeakReference<T>(source ?? throw new ArgumentNullException(nameof(source)));
		//_handler = new WeakReference<Action<T, TArgs>>(handler ?? throw new ArgumentNullException(nameof(handler)));
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