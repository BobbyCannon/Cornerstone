#region References

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Cornerstone.Keystone.Messages;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Keystone;

public abstract class KeystoneChannel<T> : KeystoneChannel where T : Enum
{
	#region Methods

	protected void Publish(T messageType)
	{
		base.Publish(System.Convert.ToInt32(messageType));
	}

	protected void Publish(T messageType, IChannelMessage message)
	{
		base.Publish(System.Convert.ToInt32(messageType), message);
	}

	protected void Subscribe(T messageType, Action handler)
	{
		base.Subscribe(System.Convert.ToInt32(messageType), handler);
	}

	protected void Subscribe<T2>(T messageType, Action<T2> handler) where T2 : IChannelMessage
	{
		base.Subscribe(System.Convert.ToInt32(messageType), handler);
	}

	protected void Unsubscribe(T messageType, Action handler)
	{
		base.Unsubscribe(System.Convert.ToInt32(messageType), handler);
	}

	protected void Unsubscribe<T2>(T messageType, Action<T2> handler) where T2 : IChannelMessage
	{
		base.Unsubscribe(System.Convert.ToInt32(messageType), handler);
	}

	#endregion
}

[SourceReflection]
public abstract class KeystoneChannel : CornerstoneObject
{
	#region Fields

	private readonly ConcurrentDictionary<int, object> _handlers;

	#endregion

	#region Constructors

	protected KeystoneChannel()
	{
		_handlers = new ConcurrentDictionary<int, object>();
	}

	#endregion

	#region Methods

	protected virtual void OnErrorOccurred(Exception obj)
	{
		ErrorOccurred?.Invoke(obj);
	}

	protected virtual void OnMessagePublished(int arg1, IChannelMessage arg2)
	{
		MessagePublished?.Invoke(arg1, arg2);
	}

	protected void Publish(int type)
	{
		MessagePublished?.Invoke(type, null);

		if (_handlers.TryGetValue(type, out var obj) && obj is IHandlerInvoker invoker)
		{
			invoker.Invoke(ErrorOccurred);
		}
	}

	protected void Publish(int type, IChannelMessage value)
	{
		MessagePublished?.Invoke(type, value);

		if (_handlers.TryGetValue(type, out var obj) && obj is IHandlerInvokerWithMessage invoker)
		{
			invoker.Invoke(value, ErrorOccurred);
		}
	}

	protected void Subscribe(int type, Action handler)
	{
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}

		HandlerList list = null;
		var done = false;

		while (!done)
		{
			if (_handlers.TryGetValue(type, out var obj))
			{
				if (obj is HandlerList typedList)
				{
					list = typedList;
					done = true;
				}
				else
				{
					throw new InvalidOperationException(
						$"Handler type conflict for message type {type}. " +
						$"Cannot subscribe because a different handler type is already registered for this message.");
				}
			}
			else
			{
				var newList = new HandlerList();
				if (_handlers.TryAdd(type, newList))
				{
					list = newList;
					done = true;
				}

				// else race condition - loop and try again
			}
		}

		list.Add(handler);
	}

	protected void Subscribe<T2>(int type, Action<T2> handler) where T2 : IChannelMessage
	{
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}

		HandlerList<T2> list = null;
		var done = false;

		while (!done)
		{
			if (_handlers.TryGetValue(type, out var obj))
			{
				if (obj is HandlerList<T2> typedList)
				{
					list = typedList;
					done = true;
				}
				else
				{
					throw new InvalidOperationException(
						$"Handler type conflict for message type {type}. " +
						$"Cannot subscribe {typeof(T2).Name} because a different handler type is already registered for this message.");
				}
			}
			else
			{
				var newList = new HandlerList<T2>();
				if (_handlers.TryAdd(type, newList))
				{
					list = newList;
					done = true;
				}

				// else race condition - loop and try again
			}
		}

		list.Add(handler);
	}

	protected void Unsubscribe(int type, Action handler)
	{
		if (handler == null)
		{
			return;
		}

		if (_handlers.TryGetValue(type, out var obj) && obj is HandlerList list)
		{
			list.Remove(handler);
			if (list.IsEmpty)
			{
				_handlers.TryRemove(type, out _);
			}
		}
	}

	protected void Unsubscribe<T2>(int type, Action<T2> handler) where T2 : IChannelMessage
	{
		if (handler == null)
		{
			return;
		}

		if (_handlers.TryGetValue(type, out var obj) && obj is HandlerList<T2> list)
		{
			list.Remove(handler);
			if (list.IsEmpty)
			{
				_handlers.TryRemove(type, out _);
			}
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// Raised when an exception occurs inside a handler.
	/// The Bus (or Engine) can subscribe to this.
	/// </summary>
	public event Action<Exception> ErrorOccurred;

	/// <summary>
	/// Optional: raised when a message is published (for history, logging, etc.)
	/// </summary>
	public event Action<int, IChannelMessage> MessagePublished;

	#endregion

	#region Interfaces

	private interface IHandlerInvoker
	{
		#region Methods

		void Invoke(Action<Exception> onError);

		#endregion
	}

	private interface IHandlerInvokerWithMessage
	{
		#region Methods

		void Invoke(IChannelMessage message, Action<Exception> onError);

		#endregion
	}

	#endregion

	#region Classes

	private sealed class HandlerList : IHandlerInvoker
	{
		#region Fields

		private readonly List<Action> _handlers;
		private readonly object _lock;

		#endregion

		#region Constructors

		public HandlerList()
		{
			_lock = new object();
			_handlers = new List<Action>();
		}

		#endregion

		#region Properties

		public bool IsEmpty
		{
			get
			{
				lock (_lock)
				{
					return _handlers.Count == 0;
				}
			}
		}

		#endregion

		#region Methods

		public void Add(Action handler)
		{
			lock (_lock)
			{
				_handlers.Add(handler);
			}
		}

		public void InvokeAll(Action<Exception> onError)
		{
			Action[] snapshot;
			int count;

			lock (_lock)
			{
				count = _handlers.Count;
				if (count == 0)
				{
					return;
				}

				snapshot = ArrayPool<Action>.Shared.Rent(count);
				_handlers.CopyTo(snapshot, 0);
			}

			try
			{
				for (var i = 0; i < count; i++)
				{
					try
					{
						snapshot[i]();
					}
					catch (Exception ex)
					{
						onError?.Invoke(ex);
					}
				}
			}
			finally
			{
				ArrayPool<Action>.Shared.Return(snapshot);
			}
		}

		public void Remove(Action handler)
		{
			lock (_lock)
			{
				_handlers.RemoveAll(x => x == handler);
			}
		}

		void IHandlerInvoker.Invoke(Action<Exception> onError)
		{
			try
			{
				InvokeAll(onError);
			}
			catch (Exception ex)
			{
				onError?.Invoke(ex);
			}
		}

		#endregion
	}

	private sealed class HandlerList<T> : IHandlerInvokerWithMessage where T : IChannelMessage
	{
		#region Fields

		private readonly List<Action<T>> _handlers;
		private readonly object _lock;

		#endregion

		#region Constructors

		public HandlerList()
		{
			_lock = new object();
			_handlers = new List<Action<T>>();
		}

		#endregion

		#region Properties

		public bool IsEmpty
		{
			get
			{
				lock (_lock)
				{
					return _handlers.Count == 0;
				}
			}
		}

		#endregion

		#region Methods

		public void Add(Action<T> handler)
		{
			lock (_lock)
			{
				_handlers.Add(handler);
			}
		}

		public void InvokeAll(IChannelMessage message, Action<Exception> onError)
		{
			Action<T>[] snapshot;
			int count;

			lock (_lock)
			{
				count = _handlers.Count;
				if (count == 0)
				{
					return;
				}

				snapshot = ArrayPool<Action<T>>.Shared.Rent(count);
				_handlers.CopyTo(snapshot, 0);
			}

			try
			{
				for (var i = 0; i < count; i++)
				{
					try
					{
						snapshot[i]((T) message);
					}
					catch (Exception ex)
					{
						onError?.Invoke(ex);
					}
				}
			}
			finally
			{
				ArrayPool<Action<T>>.Shared.Return(snapshot);
			}
		}

		public void Remove(Action<T> handler)
		{
			lock (_lock)
			{
				_handlers.RemoveAll(x => x == handler);
			}
		}

		void IHandlerInvokerWithMessage.Invoke(IChannelMessage message, Action<Exception> onError)
		{
			try
			{
				InvokeAll(message, onError);
			}
			catch
			{
				onError?.Invoke(new InvalidCastException(
					$"Message type mismatch. Expected {typeof(T).Name} but received {message.GetType().Name}."));
			}
		}

		#endregion
	}

	#endregion
}