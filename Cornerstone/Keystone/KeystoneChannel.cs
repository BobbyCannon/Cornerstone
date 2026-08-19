#region References

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Cornerstone.Keystone.Messages;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Keystone;

[SourceReflection]
public abstract class KeystoneChannel : CornerstoneObject
{
	#region Fields

	private readonly ConcurrentDictionary<Type, object> _handlers;
	private readonly string _channelName;

	#endregion

	#region Constructors

	protected KeystoneChannel()
	{
		_handlers = new ConcurrentDictionary<Type, object>();
		_channelName = GetType().Name;
	}

	#endregion

	#region Methods

	protected virtual void OnErrorOccurred(Exception obj)
	{
		ErrorOccurred?.Invoke(obj);
	}

	protected virtual void OnMessageCompleted(ChannelMessagePublishResult result)
	{
		MessageCompleted?.Invoke(result);
	}

	protected virtual void OnMessagePublished(Type messageType, IChannelMessage message)
	{
		MessagePublished?.Invoke(messageType, message);
	}

	public void Publish<T>(T message) where T : IChannelMessage
	{
		PublishCore(typeof(T), message);
	}

	public void Subscribe<T>(Action<T> handler) where T : IChannelMessage
	{
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}

		HandlerList<T> list = null;
		var done = false;

		while (!done)
		{
			if (_handlers.TryGetValue(typeof(T), out var obj))
			{
				if (obj is HandlerList<T> typedList)
				{
					list = typedList;
					done = true;
				}
				else
				{
					throw new InvalidOperationException(
						$"Handler type conflict for message {typeof(T).Name}.");
				}
			}
			else
			{
				var newList = new HandlerList<T>();
				if (_handlers.TryAdd(typeof(T), newList))
				{
					list = newList;
					done = true;
				}
			}
		}

		list.Add(handler);
	}

	public void Unsubscribe<T>(Action<T> handler) where T : IChannelMessage
	{
		if (handler == null)
		{
			return;
		}

		if (_handlers.TryGetValue(typeof(T), out var obj) && obj is HandlerList<T> list)
		{
			list.Remove(handler);
			if (list.IsEmpty)
			{
				_handlers.TryRemove(typeof(T), out _);
			}
		}
	}

	private void PublishCore(Type messageType, IChannelMessage value)
	{
		MessagePublished?.Invoke(messageType, value);

		var completed = MessageCompleted;
		if (completed is null)
		{
			if (_handlers.TryGetValue(messageType, out var withMsg) && withMsg is IHandlerInvokerWithMessage withInvoker)
			{
				withInvoker.Invoke(value, ErrorOccurred);
			}

			return;
		}

		var hadError = false;
		var errorMessage = string.Empty;
		Action<Exception> onError = ex =>
		{
			hadError = true;
			if ((errorMessage.Length == 0) && (ex is not null))
			{
				errorMessage = ex.Message ?? string.Empty;
			}

			ErrorOccurred?.Invoke(ex);
		};

		var handlerCount = 0;
		var start = Stopwatch.GetTimestamp();

		if (_handlers.TryGetValue(messageType, out var withMsg2) && withMsg2 is IHandlerInvokerWithMessage withInvoker2)
		{
			handlerCount = withInvoker2.Invoke(value, onError);
		}

		var elapsed = Stopwatch.GetElapsedTime(start);
		OnMessageCompleted(new ChannelMessagePublishResult(
			_channelName,
			messageType.Name,
			value,
			elapsed.Ticks,
			handlerCount,
			hadError,
			errorMessage));
	}

	#endregion

	#region Events

	/// <summary>
	/// Raised when an exception occurs inside a handler.
	/// The Bus (or Engine) can subscribe to this.
	/// </summary>
	public event Action<Exception> ErrorOccurred;

	/// <summary>
	/// Raised after handlers complete when at least one subscriber is attached.
	/// Used for diagnostics history (duration, handler count, errors).
	/// Prefer not to subscribe in production unless recording is intentional.
	/// </summary>
	public event Action<ChannelMessagePublishResult> MessageCompleted;

	/// <summary>
	/// Raised when a message is published, before handlers run (logging / observers).
	/// </summary>
	public event Action<Type, IChannelMessage> MessagePublished;

	#endregion

	#region Interfaces

	private interface IHandlerInvokerWithMessage
	{
		#region Methods

		int Invoke(IChannelMessage message, Action<Exception> onError);

		#endregion
	}

	#endregion

	#region Classes

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

		public int InvokeAll(IChannelMessage message, Action<Exception> onError)
		{
			Action<T>[] snapshot;
			int count;

			lock (_lock)
			{
				count = _handlers.Count;
				if (count == 0)
				{
					return 0;
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

			return count;
		}

		public void Remove(Action<T> handler)
		{
			lock (_lock)
			{
				_handlers.RemoveAll(x => x == handler);
			}
		}

		int IHandlerInvokerWithMessage.Invoke(IChannelMessage message, Action<Exception> onError)
		{
			try
			{
				return InvokeAll(message, onError);
			}
			catch
			{
				onError?.Invoke(new InvalidCastException(
					$"Message type mismatch. Expected {typeof(T).Name} but received {message.GetType().Name}."));
				return 0;
			}
		}

		#endregion
	}

	#endregion
}
