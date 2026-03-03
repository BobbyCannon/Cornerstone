#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Represents a Gap Buffer
/// </summary>
/// <typeparam name="T"> The type of the item contained in the buffer. </typeparam>
public class GapBuffer<T> : Buffer<T>
{
	#region Constants

	/// <summary>
	/// The default capacity of the buffer.
	/// </summary>
	public const int DefaultCapacity = 256;

	#endregion

	#region Fields

	private T[] _buffer;
	private int _gapEndIndex;
	private int _gapSize;
	private int _gapStartIndex;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the buffer.
	/// </summary>
	public GapBuffer() : this(DefaultCapacity)
	{
	}

	/// <summary>
	/// Initialize the buffer.
	/// </summary>
	public GapBuffer(params T[] value) : this(DefaultCapacity, value)
	{
	}

	/// <summary>
	/// Initialize the buffer.
	/// </summary>
	public GapBuffer(uint capacity, ReadOnlySpan<T> value) : this(capacity)
	{
		if (value.Length > 0)
		{
			Add(value);
		}
	}

	/// <summary>
	/// Initialize the buffer.
	/// </summary>
	public GapBuffer(uint capacity)
	{
		_buffer = new T[capacity <= 0 ? DefaultCapacity : capacity];
		_gapEndIndex = _buffer.Length;
		_gapStartIndex = 0;
		_gapSize = _buffer.Length;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Amount of physical space currently allocated.
	/// </summary>
	public int Capacity => _buffer.Length;

	public override int Count => _buffer.Length - _gapSize;

	public override bool IsReadOnly => false;

	public override T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if ((uint) index >= (uint) Count)
			{
				Babel.ThrowIndexOutOfRange(index);
			}

			return index >= _gapStartIndex ? _buffer[index + _gapSize] : _buffer[index];
		}
		set
		{
			if ((uint) index >= (uint) Count)
			{
				Babel.ThrowIndexOutOfRange(index);
			}
			if (index >= _gapStartIndex)
			{
				_buffer[index + _gapSize] = value;
			}
			else
			{
				_buffer[index] = value;
			}
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Appends a range of items to the end of the buffer.
	/// </summary>
	/// <param name="items"> The array containing the items to append. </param>
	/// <param name="index"> The starting index in the items array. </param>
	/// <param name="length"> The number of items to append. </param>
	public override void Add(ReadOnlySpan<T> items, int index, int length)
	{
		if ((length <= 0) || (index < 0) || ((index + length) > items.Length))
		{
			return;
		}

		// If gap is not at the end, move it to the end for efficient appending
		if (_gapEndIndex != _buffer.Length)
		{
			MoveGap(Count);
		}

		// Check if there's enough capacity
		if (length > _gapSize)
		{
			var requiredCapacity = _buffer.Length + (length - _gapSize);

			// Best combination for text editor use-case:
			// - Exponential growth (classic amortized good behavior)
			// - Additional proportional slack when adding small amounts (typing/pasting)
			var newCapacity = Math.Max(requiredCapacity, _buffer.Length * 2);
			newCapacity = Math.Max(newCapacity, _buffer.Length + (length * 2));

			// Safety: never go below what's strictly required
			newCapacity = Math.Max(newCapacity, requiredCapacity);
			var newBuffer = new T[newCapacity];

			// Copy data before the gap
			Array.Copy(_buffer, 0, newBuffer, 0, _gapStartIndex);

			// Update buffer and gap indices
			_buffer = newBuffer;
			_gapEndIndex = newCapacity;
			_gapSize = newCapacity - _gapStartIndex;
		}

		items.Slice(index, length).CopyTo(_buffer.AsSpan(_gapStartIndex, length));

		_gapStartIndex += length;
		_gapSize -= length;
	}

	public override void Clear()
	{
		Array.Clear(_buffer, 0, _buffer.Length);

		_gapStartIndex = 0;
		_gapEndIndex = _buffer.Length;
		_gapSize = _buffer.Length;
	}

	public override bool Contains(T item)
	{
		return (Array.IndexOf(_buffer, item, 0, _gapStartIndex) >= 0)
			|| (Array.IndexOf(_buffer, item, _gapEndIndex, _buffer.Length - _gapEndIndex) >= 0);
	}

	public override void CopyTo(T[] array, int arrayIndex)
	{
		var pos = arrayIndex;

		for (var i = 0; i < _gapStartIndex; i++)
		{
			array[pos++] = _buffer[i];
		}

		for (var i = _gapEndIndex; i < _buffer.Length; i++)
		{
			array[pos++] = _buffer[i];
		}
	}

	/// <summary>
	/// Copies a range of items from the logical content of the buffer to the provided array.
	/// </summary>
	/// <param name="startIndex"> The zero-based starting position in the logical content to begin copying from. </param>
	/// <param name="array"> The array to copy elements into. </param>
	/// <param name="arrayIndex"> The zero-based index in the destination array at which copying begins. </param>
	/// <param name="length"> The number of elements to copy. </param>
	public void CopyTo(int startIndex, T[] array, int arrayIndex, int length)
	{
		if (startIndex < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(startIndex));
		}
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(length));
		}
		if (array == null)
		{
			throw new ArgumentNullException(nameof(array));
		}
		if (arrayIndex < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));
		}
		if ((arrayIndex + length) > array.Length)
		{
			throw new ArgumentException("Destination array is too small", nameof(array));
		}

		var logicalLength = _gapStartIndex + (_buffer.Length - _gapEndIndex);
		if (startIndex > logicalLength)
		{
			throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index exceeds logical length");
		}
		if ((startIndex + length) > logicalLength)
		{
			throw new ArgumentOutOfRangeException(nameof(length), "Requested range exceeds logical length");
		}

		if (length == 0)
		{
			return;
		}

		// Fast path: everything is before the gap
		if ((startIndex + length) <= _gapStartIndex)
		{
			Array.Copy(_buffer, startIndex, array, arrayIndex, length);
			return;
		}

		// Fast path: everything is after the gap
		if (startIndex >= _gapStartIndex)
		{
			var sourceOffset = _gapEndIndex + (startIndex - _gapStartIndex);
			Array.Copy(_buffer, sourceOffset, array, arrayIndex, length);
			return;
		}

		// Copy from start to end of left part;
		var leftCount = _gapStartIndex - startIndex;
		Array.Copy(_buffer, startIndex, array, arrayIndex, leftCount);

		// Copy from right part (after gap) of the remaining elements.
		var remaining = length - leftCount;
		if (remaining > 0)
		{
			Array.Copy(_buffer, _gapEndIndex, array, arrayIndex + leftCount, remaining);
		}
	}

	public override IEnumerator<T> GetEnumerator()
	{
		var offset = 0;

		for (var i = 0; i < Count; i++)
		{
			if (i == _gapStartIndex)
			{
				offset = _gapEndIndex - _gapStartIndex;
			}

			yield return _buffer[i + offset];
		}
	}

	public ReadOnlySpans GetTwoSpans(int logicalOffset, int logicalLength)
	{
		if (logicalLength < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(logicalLength));
		}
		if ((logicalOffset < 0) || (logicalOffset > Count))
		{
			throw new ArgumentOutOfRangeException(nameof(logicalOffset));
		}
		if ((logicalOffset + logicalLength) > Count)
		{
			throw new ArgumentOutOfRangeException(nameof(logicalLength), "Range exceeds buffer content");
		}

		if (logicalLength == 0)
		{
			return new ReadOnlySpans(default, default);
		}

		var gapStart = _gapStartIndex;
		var gapEnd = _gapEndIndex;

		if ((logicalOffset + logicalLength) <= gapStart)
		{
			return new ReadOnlySpans(
				_buffer.AsSpan(logicalOffset, logicalLength),
				default
			);
		}

		if (logicalOffset >= gapStart)
		{
			var physicalStart = gapEnd + (logicalOffset - gapStart);
			return new ReadOnlySpans(
				_buffer.AsSpan(physicalStart, logicalLength),
				default
			);
		}

		var lenBefore = gapStart - logicalOffset;
		var lenAfter = logicalLength - lenBefore;
		var spanBefore = _buffer.AsSpan(logicalOffset, lenBefore);
		var spanAfter = _buffer.AsSpan(gapEnd, lenAfter);

		return new ReadOnlySpans(spanBefore, spanAfter);
	}

	public override int IndexOf(T item)
	{
		// Search before the gap.
		var foundAt = Array.IndexOf(_buffer, item, 0, _gapStartIndex);
		if (foundAt > -1)
		{
			return foundAt;
		}

		// Still here? Search after the gap.
		foundAt = Array.IndexOf(_buffer, item, _gapEndIndex, _buffer.Length - _gapEndIndex);
		if (foundAt > -1)
		{
			return foundAt - _gapSize;
		}

		// Still here? Then there's no match anywhere.
		return -1;
	}

	public override int IndexOf(T item, int index, int length)
	{
		int foundAt;

		// Start index is after the first gap
		if (index >= _gapStartIndex)
		{
			foundAt = Array.IndexOf(_buffer, item, index + _gapSize, length);
			if (foundAt > -1)
			{
				return foundAt - _gapSize;
			}
		}

		// Search before the gap.
		if ((index >= 0) && (index < _gapStartIndex))
		{
			foundAt = Array.IndexOf(_buffer, item, index, length);
			if (foundAt > -1)
			{
				return foundAt;
			}

			var difference = _gapStartIndex - index;
			length -= difference;
		}

		if ((length <= 0) || (_gapEndIndex == Capacity))
		{
			return -1;
		}

		// Still here? Search after the gap.
		foundAt = Array.IndexOf(_buffer, item, _gapEndIndex, length);
		if (foundAt > -1)
		{
			return foundAt - _gapSize;
		}

		// Still here? Then there's no match anywhere.
		return -1;
	}

	public override void Insert(int index, T item)
	{
		InternalInsert(index, [item]);
	}

	public override T[] Read(int index, int length)
	{
		var response = new T[length];
		CopyTo(index, response, 0, length);
		return response;
	}

	public override bool Remove(T item)
	{
		var index = IndexOf(item);
		if (index < 0)
		{
			return false;
		}

		RemoveAt(index);
		return true;
	}

	public override void RemoveAt(int index)
	{
		if ((uint) index >= (uint) Count)
		{
			Babel.ThrowIndexOutOfRange(index);
		}

		// If index is just after the gap, expand gap backward
		if (index == (_gapStartIndex - 1))
		{
			_buffer[index] = default!;
			_gapStartIndex--;
			_gapSize++;
			return;
		}

		// If index is just before the gap end, expand gap forward
		if (index == (_gapEndIndex - _gapSize))
		{
			_buffer[index] = default!;
			_gapEndIndex++;
			_gapSize++;
			return;
		}

		// Otherwise, move the gap to the index and expand it
		MoveGap(index);

		_buffer[_gapEndIndex] = default!;
		_gapEndIndex++;
		_gapSize++;
	}

	public T[] ToArray()
	{
		var buffer = new T[Count];
		CopyTo(buffer, 0);
		return buffer;
	}

	public override string ToString()
	{
		if (this is not GapBuffer<char> charBuffer)
		{
			return base.ToString()!;
		}

		if (Count == 0)
		{
			return string.Empty;
		}

		var buffer = new char[charBuffer.Count];
		charBuffer.CopyTo(buffer, 0);
		return new string(buffer);
	}

	/// <summary>
	/// Inserts multiple items into the buffer.
	/// </summary>
	protected void InternalInsert(int index, IEnumerable<T> value)
	{
		var array = value.ToArray();
		InternalInsert(index, array, 0, array.Length);
	}

	/// <summary>
	/// Inserts multiple items into the buffer.
	/// </summary>
	protected void InternalInsert(int index, T[] value)
	{
		InternalInsert(index, value, 0, value.Length);
	}

	/// <summary>
	/// Inserts multiple items into the buffer.
	/// </summary>
	protected void InternalInsert(int index, T[] value, int valueIndex, int valueLength)
	{
		if ((value == null) || (value.Length == 0))
		{
			return;
		}

		if ((index < 0) || (index > Count))
		{
			throw new IndexOutOfRangeException(Babel.Tower[BabelKeys.IndexOutOfRange]);
		}

		MoveGap(index);

		if (valueLength > _gapSize)
		{
			var requiredCapacity = _buffer.Length + (valueLength - _gapSize);
			var newCapacity = Math.Max(requiredCapacity, _buffer.Length * 2);
			var newBuffer = new T[newCapacity];

			Array.Copy(_buffer, 0, newBuffer, 0, _gapStartIndex);
			var dataAfterGap = _buffer.Length - _gapEndIndex;
			Array.Copy(_buffer, _gapEndIndex, newBuffer, newCapacity - dataAfterGap, dataAfterGap);

			_buffer = newBuffer;
			_gapEndIndex = newCapacity - dataAfterGap;
			_gapSize = _gapEndIndex - _gapStartIndex;
		}

		Array.Copy(value, valueIndex, _buffer, _gapStartIndex, valueLength);

		_gapStartIndex += valueLength;
		_gapSize -= valueLength;
	}

	/// <summary>
	/// Inserts multiple items into the buffer.
	/// </summary>
	protected void InternalInsert(int index, ReadOnlySpan<T> value, int valueIndex, int valueLength)
	{
		if (value.Length == 0)
		{
			return;
		}

		if ((index < 0) || (index > Count))
		{
			throw new IndexOutOfRangeException(Babel.Tower[BabelKeys.IndexOutOfRange]);
		}

		MoveGap(index);

		if (valueLength > _gapSize)
		{
			var requiredCapacity = _buffer.Length + (valueLength - _gapSize);
			var newCapacity = Math.Max(requiredCapacity, _buffer.Length * 2);
			var newBuffer = new T[newCapacity];

			Array.Copy(_buffer, 0, newBuffer, 0, _gapStartIndex);
			var dataAfterGap = _buffer.Length - _gapEndIndex;
			Array.Copy(_buffer, _gapEndIndex, newBuffer, newCapacity - dataAfterGap, dataAfterGap);

			_buffer = newBuffer;
			_gapEndIndex = newCapacity - dataAfterGap;
			_gapSize = _gapEndIndex - _gapStartIndex;
		}

		value.Slice(valueIndex, valueLength)
			.CopyTo(_buffer.AsSpan(_gapStartIndex, valueLength));

		_gapStartIndex += valueLength;
		_gapSize -= valueLength;
	}

	/// <summary>
	/// Repositions the gap in the buffer.
	/// </summary>
	private void MoveGap(int index)
	{
		if (index == _gapStartIndex)
		{
			return;
		}

		var bufferSpan = _buffer.AsSpan();

		if (index < _gapStartIndex)
		{
			var offset = _gapStartIndex - index;

			bufferSpan
				.Slice(index, offset)
				.CopyTo(bufferSpan.Slice(_gapEndIndex - offset, offset));

			_gapStartIndex -= offset;
			_gapEndIndex -= offset;
		}
		else
		{
			var count = index - _gapStartIndex;
			bufferSpan
				.Slice(_gapEndIndex, count)
				.CopyTo(bufferSpan.Slice(_gapStartIndex, count));

			_gapStartIndex += count;
			_gapEndIndex += count;
		}

		// Now the gap is in the right place → clear only if needed
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			bufferSpan
				.Slice(_gapStartIndex, _gapEndIndex - _gapStartIndex)
				.Clear();
		}

		_gapSize = _gapEndIndex - _gapStartIndex;
	}

	#endregion

	#region Structures

	public readonly ref struct ReadOnlySpans
	{
		#region Fields

		public readonly ReadOnlySpan<T> AfterGap;
		public readonly ReadOnlySpan<T> BeforeGap;

		#endregion

		#region Constructors

		public ReadOnlySpans(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
		{
			BeforeGap = a;
			AfterGap = b;
		}

		#endregion

		#region Properties

		public bool IsContiguous => AfterGap.IsEmpty;

		public int TotalLength => BeforeGap.Length + AfterGap.Length;

		#endregion
	}

	#endregion
}