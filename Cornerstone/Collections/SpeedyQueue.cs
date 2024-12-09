#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation;
using Cornerstone.Threading;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Collections;

/// <inheritdoc cref="ISpeedyQueue{T}" />
public class SpeedyQueue<T> : ReaderWriterLockBindable, ISpeedyQueue<T>, IEnumerable<T>
{
	#region Fields

	private int _limit;
	private readonly List<T> _list;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the queue.
	/// </summary>
	public SpeedyQueue() : this(null, null)
	{
	}

	/// <summary>
	/// Create an instance of the queue.
	/// </summary>
	/// <param name="readerWriterLock"> An optional lock. Defaults to <see cref="ReaderWriterLockTiny" /> if not provided. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public SpeedyQueue(IReaderWriterLock readerWriterLock, IDispatcher dispatcher) : base(readerWriterLock, dispatcher)
	{
		_list = new List<T>();
		_limit = 0;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public int Count
	{
		get
		{
			try
			{
				EnterReadLock();
				return _list.Count;
			}
			finally
			{
				ExitReadLock();
			}
		}
	}

	/// <inheritdoc />
	public bool IsEmpty
	{
		get
		{
			try
			{
				EnterReadLock();
				return _list.Count <= 0;
			}
			finally
			{
				ExitReadLock();
			}
		}
	}

	/// <inheritdoc />
	public int Limit
	{
		get => _limit;
		set
		{
			try
			{
				EnterWriteLock();
				_limit = value;
				InternalEnforceLimit(true);
			}
			finally
			{
				ExitWriteLock();
			}
		}
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Clear()
	{
		try
		{
			EnterWriteLock();

			_list.Clear();
		}
		finally
		{
			ExitWriteLock();
			OnQueueChanged();
		}
	}

	/// <inheritdoc />
	public T Enqueue(T value)
	{
		try
		{
			EnterWriteLock();

			_list.Add(value);

			InternalEnforceLimit(true);

			return value;
		}
		finally
		{
			ExitWriteLock();
			OnQueueChanged();
		}
	}

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
	{
		try
		{
			EnterReadLock();
			var list = _list.ToList();
			return list.GetEnumerator();
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <inheritdoc />
	public bool TryDequeue(out T value)
	{
		if (IsEmpty)
		{
			value = default;
			return false;
		}

		try
		{
			EnterWriteLock();

			if (_list.Count <= 0)
			{
				value = default;
				return false;
			}

			value = _list[0];
			_list.RemoveAt(0);
			return true;
		}
		finally
		{
			ExitWriteLock();
			OnQueueChanged();
		}
	}

	/// <inheritdoc />
	public bool TryPeek(out T value)
	{
		if (IsEmpty)
		{
			value = default;
			return false;
		}

		try
		{
			EnterReadLock();

			if (_list.Count <= 0)
			{
				value = default;
				return false;
			}

			value = _list[0];
			return true;
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <summary>
	/// Triggers the QueueChanged event.
	/// </summary>
	[SuppressPropertyChangedWarnings]
	protected virtual void OnQueueChanged()
	{
		QueueChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private void InternalEnforceLimit(bool start)
	{
		if (_limit <= 0)
		{
			return;
		}

		while (_list.Count > Limit)
		{
			var index = start ? 0 : _list.Count - 1;
			_list.RemoveAt(index);
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// The queue has changed.
	/// </summary>
	public event EventHandler QueueChanged;

	#endregion
}

/// <summary>
/// A thread-safe, dispatch safe, and limitable queue.
/// Dispatch safe and limit settings are optional.
/// </summary>
/// <typeparam name="T"> The type of items in the queue. </typeparam>
public interface ISpeedyQueue<T>
{
	#region Properties

	/// <summary>
	/// Gets the number of elements contained in the queue.
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Gets a value that indicates whether the queue is empty.
	/// </summary>
	bool IsEmpty { get; }

	/// <summary>
	/// The maximum limit for this queue
	/// </summary>
	int Limit { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Clear the queue.
	/// </summary>
	void Clear();

	/// <summary>
	/// Adds an object to the queue.
	/// </summary>
	/// <param name="value"> The value to be added. </param>
	T Enqueue(T value);

	/// <summary>
	/// Tries to read and remove the first item at the beginning of the queue.
	/// </summary>
	/// <param name="value"> The value that was dequeued. </param>
	/// <returns> Returns true if a value was read and dequeued otherwise false. </returns>
	bool TryDequeue(out T value);

	/// <summary>
	/// Tries to read the first item at the beginning of the queue.
	/// The item is not removed from the queue.
	/// </summary>
	/// <param name="value"> The value that is next in the queue. </param>
	/// <returns> Returns true if a value was read otherwise false. </returns>
	bool TryPeek(out T value);

	#endregion
}