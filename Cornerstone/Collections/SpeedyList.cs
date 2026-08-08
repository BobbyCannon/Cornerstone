#region References

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Collections;

public class SpeedyList<T> : SpeedyList, IList<T>, IReadOnlyList<T>, IDispatchPending, IDisposable
{
	#region Fields

	private T[] _buffer;
	private readonly bool _clearOnCleanup;
	private bool _disposed;
	private int _dispatchPending;
	private readonly bool _isRented;

	[ThreadStatic]
	private static SpeedyList<T> _threadLocalBuffer;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiates a list that focuses on performance.
	/// </summary>
	/// <param name="initialCapacity"> The initial size. </param>
	/// <param name="isLongLivedBuffer"> Set true only if the buffer is used long term (minutes+). </param>
	/// <param name="clearOnCleanup"> Optional to clear value types on cleanup (clear/dispose). </param>
	public SpeedyList(
		int initialCapacity = DefaultCapacity,
		bool isLongLivedBuffer = false,
		bool clearOnCleanup = false)
	{
		if (initialCapacity < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(initialCapacity));
		}

		_clearOnCleanup = clearOnCleanup || RuntimeHelpers.IsReferenceOrContainsReferences<T>();
		_isRented = !isLongLivedBuffer;
		_buffer = _isRented ? ArrayPool<T>.Shared.Rent(initialCapacity) : new T[initialCapacity];
		_disposed = false;

		ReadPosition = 0;
		WritePosition = 0;
	}

	#endregion

	#region Properties

	public int Capacity
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _buffer.Length;
	}

	public int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => WritePosition;
	}

	/// <inheritdoc />
	public bool HasPending
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Volatile.Read(ref _dispatchPending) != 0;
	}

	public bool IsReadOnly => false;

	public T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if ((uint) index >= (uint) WritePosition)
			{
				throw new IndexOutOfRangeException(nameof(index));
			}

			return _buffer[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set
		{
			if ((uint) index >= (uint) WritePosition)
			{
				throw new IndexOutOfRangeException(nameof(index));
			}

			_buffer[index] = value;
			MarkPending();
		}
	}

	public int ReadPosition
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private set;
	}

	public int Remaining
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _buffer.Length - WritePosition;
	}

	public int WritePosition
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private set;
	}

	#endregion

	#region Methods

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(T value)
	{
		var pos = WritePosition;
		var buf = _buffer;

		// Fast-path: skip EnsureCapacity call when there is room
		if ((uint) pos < (uint) buf.Length)
		{
			buf[pos] = value;
			WritePosition = pos + 1;
			MarkPending();
			return;
		}
		AddWithResize(value);
		MarkPending();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(ReadOnlySpan<T> data)
	{
		if (data.IsEmpty)
		{
			return;
		}

		var newEnd = WritePosition + data.Length;
		if (newEnd <= _buffer.Length)
		{
			// Fast-path: no resize needed
			data.CopyTo(_buffer.AsSpan(WritePosition));
			WritePosition = newEnd;
			MarkPending();
			return;
		}
		AddSpanWithResize(data);
		MarkPending();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(T[] data, int offset, int count)
	{
		if (data == null)
		{
			throw new ArgumentNullException(nameof(data));
		}
		if ((offset < 0) || (count <= 0) || ((offset + count) > data.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}

		Add(data.AsSpan(offset, count));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<T> AsSpan()
	{
		return _buffer.AsSpan(0, Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<T> AsSpan(int start)
	{
		return _buffer.AsSpan(start, Count - start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<T> AsSpan(int start, int length)
	{
		if ((start < 0) || (length <= 0) || ((start + length) > Count))
		{
			throw new ArgumentOutOfRangeException(nameof(length));
		}
		return _buffer.AsSpan(start, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Clear()
	{
		var hadItems = WritePosition > 0;
		if (_clearOnCleanup)
		{
			Array.Clear(_buffer, 0, WritePosition);
		}
		ReadPosition = 0;
		WritePosition = 0;
		if (hadItems)
		{
			MarkPending();
		}
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ClearHasPending()
	{
		Volatile.Write(ref _dispatchPending, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(T item)
	{
		return IndexOf(item) >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTo(T[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException(nameof(array));
		}
		if (arrayIndex < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));
		}
		if ((array.Length - arrayIndex) < Count)
		{
			throw new ArgumentException(Babel.Tower[BabelKeys.ArgumentTooSmall]);
		}

		AsSpan().CopyTo(array.AsSpan(arrayIndex));
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		Clear();

		if (_isRented)
		{
			// No need to clear on return because the clear
			// above will pre-clear the buffer.
			ArrayPool<T>.Shared.Return(_buffer, _clearOnCleanup);
		}

		_buffer = null;
		_disposed = true;
	}

	/// <summary>
	/// Returns a struct enumerator to avoid heap allocations.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	/// <summary>
	/// This is for local thread specific buffer.
	/// </summary>
	/// <param name="initialCapacity"> The initial size. </param>
	/// <param name="isLongLivedBuffer"> Set true only if the buffer is used long term (minutes+). </param>
	/// <param name="clearOnCleanup"> Optional to clear value types on cleanup (clear/dispose). </param>
	public static SpeedyList<T> GetThreadLocalInstance(
		int initialCapacity = DefaultCapacity,
		bool isLongLivedBuffer = false,
		bool clearOnCleanup = false)
	{
		if ((_threadLocalBuffer == null) || _threadLocalBuffer._disposed ||
			(_threadLocalBuffer._isRented != !isLongLivedBuffer) ||
			(_threadLocalBuffer._clearOnCleanup != clearOnCleanup) ||
			(_threadLocalBuffer.Capacity < initialCapacity))
		{
			_threadLocalBuffer?.Dispose();
			_threadLocalBuffer = new SpeedyList<T>(initialCapacity, isLongLivedBuffer, clearOnCleanup);
		}
		else
		{
			_threadLocalBuffer.Clear();
		}
		return _threadLocalBuffer;
	}

	/// <summary>
	/// Returns a writable span of the requested size at the current write position,
	/// advancing WritePosition. Caller MUST write exactly <paramref name="count" /> elements.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<T> GetWriteSpan(int count)
	{
		if (count <= 0)
		{
			return Span<T>.Empty;
		}

		var pos = WritePosition;
		var newEnd = pos + count;

		if (newEnd > _buffer.Length)
		{
			Grow(newEnd);
		}

		WritePosition = newEnd;
		MarkPending();
		return _buffer.AsSpan(pos, count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int IndexOf(T item)
	{
		return Array.IndexOf(_buffer, item, 0, WritePosition);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Insert(int index, T item)
	{
		if ((uint) index > (uint) WritePosition)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		EnsureCapacity(WritePosition + 1);

		if (index < WritePosition)
		{
			// Shift elements right
			_buffer.AsSpan(index, WritePosition - index)
				.CopyTo(_buffer.AsSpan(index + 1));
		}

		_buffer[index] = item;
		WritePosition++;
		MarkPending();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Insert(int index, ReadOnlySpan<T> items)
	{
		if ((uint) index > (uint) WritePosition)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		if (items.Length == 0)
		{
			return;
		}

		EnsureCapacity(WritePosition + items.Length);

		if (index < WritePosition)
		{
			// Shift elements right
			_buffer.AsSpan(index, WritePosition - index)
				.CopyTo(_buffer.AsSpan(index + items.Length));
		}

		items.CopyTo(_buffer.AsSpan(index));
		WritePosition += items.Length;
		MarkPending();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Load(ReadOnlySpan<T> items)
	{
		Clear();
		Add(items);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T[] Read(int index, int length)
	{
		if (((uint) index > (uint) WritePosition) || (length < 0) || ((index + length) > WritePosition))
		{
			throw new ArgumentOutOfRangeException(nameof(length));
		}

		var result = new T[length];
		AsSpan(index, length).CopyTo(result.AsSpan());
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Read(int offset, T[] destination, int destinationOffset, int count)
	{
		if (destination == null)
		{
			throw new ArgumentNullException(nameof(destination));
		}
		if ((offset < 0) || (count < 0) || ((destinationOffset + count) > destination.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}
		if ((offset + count) > WritePosition)
		{
			throw new ArgumentOutOfRangeException(Babel.Tower[BabelKeys.IndexAndLengthOutOfRange]);
		}

		AsSpan(offset, count).CopyTo(destination.AsSpan(destinationOffset, count));
		ReadPosition = offset + count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Read()
	{
		var pos = ReadPosition;
		if ((uint) pos >= (uint) WritePosition)
		{
			throw new InvalidOperationException("Not enough data to read.");
		}
		ReadPosition = pos + 1;
		return _buffer[pos];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Remove(T item)
	{
		var index = IndexOf(item);
		if (index < 0)
		{
			return false;
		}
		RemoveAt(index);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RemoveAt(int index)
	{
		if ((uint) index >= (uint) WritePosition)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		WritePosition--;

		if (index < WritePosition)
		{
			_buffer.AsSpan(index + 1, WritePosition - index)
				.CopyTo(_buffer.AsSpan(index));
		}

		if (_clearOnCleanup)
		{
			_buffer[WritePosition] = default!;
		}

		MarkPending();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RemoveRange(int index, int count)
	{
		if (((uint) index > (uint) WritePosition)
			|| (count < 0) || ((index + count) > WritePosition))
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}

		if (count == 0)
		{
			return;
		}

		var remaining = WritePosition - (index + count);
		if (remaining > 0)
		{
			_buffer.AsSpan(index + count, remaining)
				.CopyTo(_buffer.AsSpan(index));
		}

		WritePosition -= count;

		if (_clearOnCleanup)
		{
			Array.Clear(_buffer, WritePosition, count); // optional but clean
		}

		MarkPending();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Seek(int position)
	{
		if ((uint) position > (uint) WritePosition) // better style, matches your other checks
		{
			throw new ArgumentOutOfRangeException(nameof(position));
		}
		ReadPosition = position;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T[] ToArray()
	{
		// Retry when Count changes mid-copy (common with producer/consumer dispatch snapshots).
		for (var attempt = 0; attempt < 16; attempt++)
		{
			var count = Count;
			if (count == 0)
			{
				return [];
			}

			var result = new T[count];
			var span = AsSpan();
			if (span.Length != count)
			{
				continue;
			}

			span.CopyTo(result.AsSpan());
			if (Count != count)
			{
				continue;
			}

			return result;
		}

		// Fallback: copy current span as-is (may still race, but avoids oversized buffer).
		var final = AsSpan();
		if (final.Length == 0)
		{
			return [];
		}

		var copy = new T[final.Length];
		final.CopyTo(copy);
		return copy;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override string ToString()
	{
		if (typeof(T) == typeof(byte))
		{
			var t = Unsafe.As<T[], byte[]>(ref _buffer);
			return Encoding.Unicode.GetString(t, 0, Count);
		}
		if (typeof(T) == typeof(char))
		{
			return new string(Unsafe.As<T[], char[]>(ref _buffer), 0, Count);
		}
		return base.ToString();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void EnsureCapacity(int neededCapacity)
	{
		if (neededCapacity <= _buffer.Length)
		{
			return;
		}

		Grow(neededCapacity);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void AddSpanWithResize(ReadOnlySpan<T> data)
	{
		EnsureCapacity(WritePosition + data.Length);
		data.CopyTo(_buffer.AsSpan(WritePosition));
		WritePosition += data.Length;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void AddWithResize(T value)
	{
		EnsureCapacity(WritePosition + 1);
		_buffer[WritePosition++] = value;
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		for (var i = 0; i < Count; i++)
		{
			yield return _buffer[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<T>) this).GetEnumerator();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Grow(int neededCapacity)
	{
		var newCapacity = Math.Max(neededCapacity, _buffer.Length * 2);
		var newBuffer = _isRented
			? ArrayPool<T>.Shared.Rent(newCapacity)
			: new T[newCapacity];

		AsSpan().CopyTo(newBuffer.AsSpan());

		if (_isRented)
		{
			ArrayPool<T>.Shared.Return(_buffer, _clearOnCleanup);
		}
		else if (_clearOnCleanup)
		{
			Array.Clear(_buffer, 0, _buffer.Length);
		}

		_buffer = newBuffer;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void MarkPending()
	{
		if (Volatile.Read(ref _dispatchPending) == 0)
		{
			Volatile.Write(ref _dispatchPending, 1);
		}
	}

	#endregion

	#region Structures

	/// <summary>
	/// Allocation-free struct enumerator for foreach usage.
	/// </summary>
	public struct Enumerator
	{
		#region Fields

		private int _index;
		private readonly SpeedyList<T> _list;

		#endregion

		#region Constructors

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Enumerator(SpeedyList<T> list)
		{
			_list = list;
			_index = -1;
		}

		#endregion

		#region Properties

		public T Current
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _list._buffer[_index];
		}

		#endregion

		#region Methods

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MoveNext()
		{
			var next = _index + 1;
			if (next < _list.WritePosition)
			{
				_index = next;
				return true;
			}
			return false;
		}

		#endregion
	}

	#endregion
}

public class SpeedyList
{
	#region Constants

	public const int DefaultCapacity = 16384;

	#endregion
}