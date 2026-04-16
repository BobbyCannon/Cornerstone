#region References

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Collections;

public class SpeedyQueue<T> : Notifiable, IQueue<T>
{
	#region Fields

	private T[] _buffer;
	private int _count;
	private uint _head;
	private uint _mask;
	private int _maxItems;
	private uint _tail;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new SpeedyQueue.
	/// </summary>
	/// <param name="initialCapacity"> Initial capacity. Must be a power of 2 and ≥ 4. </param>
	/// <param name="maxItems">
	/// Maximum number of items allowed in the queue.
	/// - null or int.MaxValue = unlimited growth (default behavior)
	/// - Any positive number = hard limit (drops oldest items when full)
	/// </param>
	/// <param name="mode"> FIFO (default) or LIFO behavior. </param>
	public SpeedyQueue(int initialCapacity = 64, int? maxItems = null, QueueMode mode = QueueMode.FIFO)
	{
		if (((initialCapacity & (initialCapacity - 1)) != 0) || (initialCapacity < 4))
		{
			throw new ArgumentException("initialCapacity must be a power of 2 ≥ 4");
		}

		_buffer = new T[initialCapacity];
		_mask = (uint) initialCapacity - 1;
		_maxItems = maxItems ?? int.MaxValue;
		_count = 0;
		_head = 0;
		_tail = 0;

		Mode = mode;
	}

	#endregion

	#region Properties

	public int Capacity => (int) (_mask + 1);

	public int Count => Volatile.Read(ref _count);

	public bool IsEmpty => _count == 0;

	/// <summary>
	/// Maximum number of items the queue can hold.
	/// Returns null if the queue has unlimited growth.
	/// </summary>
	public int? MaxItems
	{
		get => _maxItems == int.MaxValue ? null : _maxItems;
		set
		{
			_maxItems = value ?? int.MaxValue;
			EnforceLimit();
		}
	}

	/// <summary>
	/// Gets or sets the current queue mode (FIFO or LIFO).
	/// Changing the mode at runtime is safe and does not affect the existing items
	/// (the internal buffer always maintains oldest-to-newest order).
	/// </summary>
	public QueueMode Mode { get; }

	#endregion

	#region Methods

	public void Clear()
	{
		Array.Clear(_buffer, 0, _buffer.Length);
		_head = 0;
		_tail = 0;
		_count = 0;

		OnQueueChanged();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Enqueue(T value)
	{
		InternalEnqueue(value);
		OnQueueChanged();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Enqueue(ReadOnlySpan<T> values)
	{
		if (values.IsEmpty)
		{
			return;
		}
		foreach (var item in values)
		{
			InternalEnqueue(item);
		}
		OnQueueChanged();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryDequeue(out T value)
	{
		if (_count == 0)
		{
			value = default;
			return false;
		}

		if (Mode == QueueMode.FIFO)
		{
			// Original FIFO behavior
			value = _buffer[_head];
			_buffer[_head] = default!; // ! = Help GC / avoid leaks for reference types
			_head = (_head + 1) & _mask;
		}
		else
		{
			// LIFO behavior: remove from the "top" (last enqueued item)
			var pos = (_tail - 1) & _mask;
			value = _buffer[pos];
			_buffer[pos] = default!;
			_tail = (_tail - 1) & _mask;
		}

		_count--;

		OnQueueChanged();
		return true;
	}

	protected virtual void OnQueueChanged()
	{
		OnPropertyChanged(nameof(Count));
		OnPropertyChanged(nameof(IsEmpty));
		QueueChanged?.Invoke(this, EventArgs.Empty);
	}

	private void EnforceLimit()
	{
		while (_count > _maxItems)
		{
			_buffer[_head] = default;
			_head = (_head + 1) & _mask;
			_count--;
		}
	}

	private void GrowBuffer()
	{
		var oldCap = Capacity;
		var newCap = oldCap << 1;
		var newBuffer = new T[newCap];

		// Copy elements in order (oldest first) - works for both modes
		for (var i = 0; i < _count; i++)
		{
			newBuffer[i] = _buffer[(_head + (uint) i) & _mask];
		}

		_buffer = newBuffer;
		_head = 0;
		_tail = (uint) _count;
		_mask = (uint) newCap - 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void InternalEnqueue(T value)
	{
		// Grow the buffer if:
		//   - The max items has not been hit
		//   - The buffer is physically full (_count == Capacity)
		if ((Capacity < _maxItems)
			&& (_count == Capacity))
		{
			GrowBuffer();
		}

		// Enforce hard limit: if we have a real MaxItems,
		// and we've reached it, drop oldest
		else if (_count >= (uint) _maxItems)
		{
			_buffer[_head] = default;
			_head = (_head + 1) & _mask;
			_count--;
		}

		// Now add the new item (always to the "newest" end)
		_buffer[_tail] = value;
		_tail = (_tail + 1) & _mask;
		_count++;
	}

	#endregion

	#region Events

	public event EventHandler QueueChanged;

	#endregion
}

public interface IQueue<T>
{
	#region Properties

	int Count { get; }

	#endregion

	#region Methods

	void Clear();

	void Enqueue(T value);

	void Enqueue(ReadOnlySpan<T> values);

	bool TryDequeue(out T value);

	#endregion
}