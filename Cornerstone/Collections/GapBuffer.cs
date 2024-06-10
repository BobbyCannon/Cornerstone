#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;

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
	public GapBuffer(IEnumerable<T> value) : this(value, DefaultCapacity)
	{
	}

	/// <summary>
	/// Initialize the buffer.
	/// </summary>
	public GapBuffer(IEnumerable<T> value, int capacity) : this(capacity)
	{
		if (value != null)
		{
			AddRange(value);
		}
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
	public GapBuffer(int capacity, params T[] value) : this(capacity)
	{
		AddRange(value);
	}

	/// <summary>
	/// Initialize the buffer.
	/// </summary>
	public GapBuffer(int capacity)
	{
		_buffer = new T[capacity];
		_gapEndIndex = _buffer.Length;
	}

	#endregion

	#region Properties

	/// <summary> Amount of physical space currently allocated. </summary>
	public int Capacity => _buffer.Length;

	/// <summary> Current size of the collection. </summary>
	public override int Count => _buffer.Length - GapSize;

	/// <inheritdoc />
	public override bool IsReadOnly => false;

	/// <summary> Get/set the entry at the specified position. </summary>
	public override T this[int index]
	{
		get
		{
			VerifyRange(index);
			return index >= _gapStartIndex ? _buffer[index + GapSize] : _buffer[index];
		}
		set
		{
			VerifyRange(index);
			if (index >= _gapStartIndex)
			{
				_buffer[index + GapSize] = value;
			}
			else
			{
				_buffer[index] = value;
			}
		}
	}

	/// <summary> The size of the movable gap. </summary>
	private int GapSize => _gapEndIndex - _gapStartIndex;

	#endregion

	#region Methods

	/// <summary> Add an item to the end of the collection. </summary>
	public override void Add(T item)
	{
		Insert(Count, item);
	}

	/// <summary> Adds multiple items to the end of the collection. </summary>
	public void AddRange(IEnumerable<T> items)
	{
		foreach (var item in items)
		{
			Add(item);
		}
	}

	/// <summary> Clears the collection. </summary>
	public override void Clear()
	{
		Array.Clear(_buffer, 0, _buffer.Length);

		_gapStartIndex = 0;
		_gapEndIndex = _buffer.Length;
	}

	/// <inheritdoc />
	public override bool Contains(T item)
	{
		return _buffer.Contains(item);
	}

	/// <inheritdoc />
	public override void CopyTo(T[] array, int arrayIndex)
	{
		if (_gapStartIndex == 0)
		{
			Array.Copy(_buffer, _gapEndIndex, array, arrayIndex, Count);
		}
		else
		{
			Array.Copy(_buffer, 0, array, arrayIndex, _gapStartIndex);

			if (_gapEndIndex != _buffer.Length)
			{
				Array.Copy(_buffer, _gapEndIndex, array, arrayIndex + _gapStartIndex, _buffer.Length - _gapEndIndex);
			}
		}
	}

	/// <summary>
	/// Copies a range of items from the buffer to the provided array.
	/// </summary>
	/// <param name="array"> The array to copy to. </param>
	/// <param name="arrayIndex"> The index in the array to start copying into. </param>
	/// <param name="length"> The amount of data to copy. </param>
	public void CopyTo(T[] array, int arrayIndex, int length)
	{
		if (_gapStartIndex == 0)
		{
			Array.Copy(_buffer, _gapEndIndex, array, arrayIndex, length);
		}
		else
		{
			if (length <= _gapEndIndex)
			{
				Array.Copy(_buffer, 0, array, arrayIndex, length);
				return;
			}

			Array.Copy(_buffer, 0, array, arrayIndex, _gapStartIndex);

			var remaining = length - _gapStartIndex;

			if ((remaining > 0) && (_gapEndIndex != _buffer.Length))
			{
				Array.Copy(_buffer, _gapEndIndex, array, arrayIndex + _gapStartIndex, remaining);
			}
		}
	}

	/// <inheritdoc />
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

	/// <summary>
	/// Get a range from the buffer.
	/// </summary>
	/// <param name="index"> The start index to read from. </param>
	/// <param name="length"> The length of text to read. </param>
	/// <returns> The buffer read. </returns>
	public GapBuffer<T> GetRange(int index, int length)
	{
		if (length <= 0)
		{
			return [];
		}

		VerifyRange(index, length);

		var buffer = new T[length];

		for (var i = 0; i < length; i++)
		{
			buffer[i] = this[i + index];
		}

		return new GapBuffer<T>(buffer);
	}

	/// <summary>
	/// Gets the index of the first occurrence the item.
	/// </summary>
	/// <returns> The index if found otherwise -1 if not found. </returns>
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
			return foundAt - GapSize;
		}

		// Still here? Then there's no match anywhere.
		return -1;
	}
	
	/// <summary>
	/// Gets the index of the first occurrence the item.
	/// </summary>
	/// <returns> The index if found otherwise -1 if not found. </returns>
	public override int IndexOf(T item, int startIndex, int count)
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
			return foundAt - GapSize;
		}

		// Still here? Then there's no match anywhere.
		return -1;
	}

	/// <summary>
	/// Gets the index of the first occurrence of any value in the provided values.
	/// </summary>
	/// <param name="anyOf"> The values to search for. </param>
	/// <returns> The index if found otherwise -1 if not found. </returns>
	public int IndexOfAny(T[] anyOf)
	{
		return IndexOfAny(anyOf, 0, Count);
	}

	/// <summary>
	/// Gets the index of the first occurrence of any value in the provided values in reverse.
	/// </summary>
	/// <param name="anyOf"> The values to search for. </param>
	/// <returns> The index if found otherwise -1 if not found. </returns>
	public int IndexOfAnyReverse(T[] anyOf)
	{
		return IndexOfAnyReverse(anyOf, Count - 1);
	}

	/// <summary>
	/// Gets the index of the first occurrence of any value in the provided values in reverse.
	/// </summary>
	/// <param name="anyOf"> The values to search for. </param>
	/// <param name="index"> The index to start the search. </param>
	/// <returns> The index if found otherwise -1 if not found. </returns>
	public int IndexOfAnyReverse(T[] anyOf, int index)
	{
		if (!this.ValidRange(index, 0))
		{
			return -1;
		}

		if (anyOf.Contains(this[index]))
		{
			return index;
		}

		for (var i = index - 1; i >= 0; i--)
		{
			for (var j = 0; j < anyOf.Length; j++)
			{
				if (Equals(this[i], anyOf[j]))
				{
					return i;
				}
			}
		}

		return -1;
	}

	/// <summary>
	/// Inserts an item into the collection
	/// </summary>
	public override void Insert(int index, T item)
	{
		if ((index < 0) || (index > Count))
		{
			throw new IndexOutOfRangeException(Babel.Tower[BabelKeys.IndexOutOfRange]);
		}

		MoveGap(index);
		ResizeGap(1);

		_buffer[index] = item;
		_gapStartIndex++;
	}

	/// <summary>
	/// Inserts multiple items from a subset of items into the collection.
	/// </summary>
	public void Insert(int index, T[] values, int valueStart, int valueLength)
	{
		for (var i = 0; i < valueLength; i++)
		{
			Insert(index + i, values[valueStart + i]);
		}
	}

	/// <summary>
	/// Inserts multiple items into the collection.
	/// </summary>
	public void Insert(int index, GapBuffer<T> buffer)
	{
		var i = index;
		foreach (var item in buffer)
		{
			Insert(i++, item);
		}
	}

	/// <summary>
	/// Inserts multiple items into the collection.
	/// </summary>
	public void InsertRange(int index, IEnumerable<T> items)
	{
		var i = index;
		foreach (var item in items)
		{
			Insert(i++, item);
		}
	}

	/// <summary>
	/// Inserts multiple items into the collection.
	/// </summary>
	public override void InsertRange(int index, T[] items)
	{
		var i = index;
		foreach (var item in items)
		{
			Insert(i++, item);
		}
	}

	/// <summary>
	/// Gets the index of the last occurrence of the specified item in this rope.
	/// </summary>
	/// <param name="item"> The item to search for. </param>
	/// <param name="index"> Start index of the area to search. </param>
	/// <param name="length"> Length of the area to search. </param>
	/// <returns> The last index where the item was found otherwise -1 if not found. </returns>
	public int LastIndexOf(T item, int index, int length)
	{
		VerifyRange(index, length);

		var comparer = System.Collections.Generic.EqualityComparer<T>.Default;
		for (var i = (index + length) - 1; i >= index; i--)
		{
			if (comparer.Equals(this[i], item))
			{
				return i;
			}
		}

		return -1;
	}

	public int Read(T[] buffer, int index, int count)
	{
		VerifyRange(index, count);

		int n;

		for (n = 0; n < count; n++)
		{
			var item = _buffer[index + n];
			buffer[index + n] = item;
		}

		return n;
	}

	/// <inheritdoc />
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

	/// <summary>
	/// Removes the item at the specified position.
	/// </summary>
	public override void RemoveAt(int index)
	{
		VerifyRange(index);
		MoveGap(index);

		// Allow garbage collection.
		_buffer[_gapEndIndex] = default;
		_gapEndIndex++;
	}

	/// <summary>
	/// Removes multiple items at the specified position.
	/// </summary>
	public override void RemoveRange(int index, int length)
	{
		if (length < 1)
		{
			return;
		}

		VerifyRange(index, length);

		var idx = (index + length) - 1;

		for (var i = 0; i < length; i++)
		{
			RemoveAt(idx--);
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		if (this is not GapBuffer<char> charBuffer)
		{
			return base.ToString();
		}

		if (Count == 0)
		{
			return string.Empty;
		}

		var buffer = new char[charBuffer.Count];
		charBuffer.CopyTo(buffer, 0);
		return new string(buffer);
	}

	/// <summary> Repositions the gap in the buffer. </summary>
	private void MoveGap(int index)
	{
		if (index == _gapStartIndex)
		{
			return; // Doesn't need to move.
		}
		if (GapSize == 0)
		{
			// Zero size gap.
			_gapStartIndex = _gapEndIndex = index;
			return;
		}

		if (index < _gapStartIndex)
		{
			// Move the gap backwards.
			var offset = _gapStartIndex - index;
			var sizeDiff = GapSize < offset ? GapSize : offset;
			Array.Copy(_buffer, index, _buffer, _gapEndIndex - offset, offset);
			_gapStartIndex -= offset;
			_gapEndIndex -= offset;

			// Allow garbage collection.
			Array.Clear(_buffer, index, sizeDiff);
		}
		else
		{
			// Move the gap forwards.
			var count = index - _gapStartIndex;
			var deltaIndex = index > _gapEndIndex ? index : _gapEndIndex;
			Array.Copy(_buffer, _gapEndIndex, _buffer, _gapStartIndex, count);
			_gapStartIndex += count;
			_gapEndIndex += count;

			// Allow garbage collection.
			Array.Clear(_buffer, deltaIndex, _gapEndIndex - deltaIndex);
		}
	}

	/// <summary> Resizes the gap in the buffer. </summary>
	private void ResizeGap(int requiredGapSize)
	{
		if (requiredGapSize <= GapSize)
		{
			return;
		}

		var newCapacity = (Count + requiredGapSize) * 2;
		if (newCapacity < DefaultCapacity)
		{
			newCapacity = DefaultCapacity;
		}

		var newBuffer = new T[newCapacity];
		var newGapEnd = newBuffer.Length - (_buffer.Length - _gapEndIndex);

		Array.Copy(_buffer, 0, newBuffer, 0, _gapStartIndex);
		Array.Copy(_buffer, _gapEndIndex, newBuffer, newGapEnd, newBuffer.Length - newGapEnd);

		_buffer = newBuffer;
		_gapEndIndex = newGapEnd;
	}

	#endregion
}