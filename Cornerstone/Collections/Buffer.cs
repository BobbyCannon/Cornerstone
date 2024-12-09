#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Collections;

/// <inheritdoc />
public abstract class Buffer<T> : IBuffer<T>
{
	#region Properties

	/// <summary>
	/// The amount of items in the buffer.
	/// </summary>
	/// <remarks>
	/// Named [Count] because of <see cref="ICollection" />
	/// </remarks>
	public abstract int Count { get; }

	/// <inheritdoc />
	public abstract bool IsReadOnly { get; }

	/// <inheritdoc />
	public abstract T this[int index] { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual void Add(T item)
	{
		Insert(Count, item);
	}

	/// <inheritdoc />
	public void Add(IEnumerable<T> items)
	{
		Insert(Count, items);
	}

	/// <inheritdoc />
	public abstract void Clear();

	/// <inheritdoc />
	public abstract bool Contains(T item);

	/// <inheritdoc />
	public abstract void CopyTo(T[] array, int arrayIndex);

	public virtual bool EndsWith(T item)
	{
		return (Count > 0) && Equals(this[Count - 1], item);
	}

	/// <inheritdoc />
	public abstract IEnumerator<T> GetEnumerator();

	/// <inheritdoc />
	public abstract int IndexOf(T item);

	/// <summary>
	/// Gets the index of the first occurrence the specified item.
	/// </summary>
	/// <param name="item"> Item to search for. </param>
	/// <param name="index"> The start index to search from. </param>
	/// <returns> The first index where the item was found otherwise -1 if not found. </returns>
	public int IndexOf(T item, int index)
	{
		return IndexOf(item, index, Count - index);
	}

	/// <summary>
	/// Gets the index of the first occurrence the specified item.
	/// </summary>
	/// <param name="item"> Item to search for. </param>
	/// <param name="index"> The start index to search from. </param>
	/// <param name="length"> The length of buffer to search. </param>
	/// <returns> The first index where the item was found otherwise -1 if not found. </returns>
	public abstract int IndexOf(T item, int index, int length);

	/// <inheritdoc />
	public int IndexOfAny(T[] anyOf)
	{
		return IndexOfAny(anyOf, 0, Count);
	}

	/// <summary>
	/// Gets the index of the first occurrence of any value in the provided values.
	/// </summary>
	/// <param name="anyOf"> The values to search for. </param>
	/// <param name="index"> The index to start the search. </param>
	/// <param name="length"> Length of the area to search. </param>
	/// <returns> The index if found otherwise -1 if not found. </returns>
	public int IndexOfAny(T[] anyOf, int index, int length)
	{
		if (!this.ValidRange(index, length))
		{
			return -1;
		}

		if (anyOf.Contains(this[index]))
		{
			return index;
		}

		for (var i = index; i < (index + length); i++)
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

	/// <inheritdoc />
	public void Insert(int index, IEnumerable<T> values)
	{
		Replace(index, 0, values);
	}

	/// <inheritdoc />
	public void Insert(int index, T[] values, int valueIndex, int valueLength)
	{
		Replace(index, 0, values, valueIndex, valueLength);
	}

	/// <inheritdoc />
	public abstract void Insert(int index, T item);

	/// <summary>
	/// Inserts a set of items to the buffer at the specified index.
	/// </summary>
	/// <param name="index"> The zero-based index at which item(s) should be inserted. </param>
	/// <param name="items"> The items to be inserted. </param>
	public abstract void Insert(int index, T[] items);

	/// <inheritdoc />
	public virtual T[] Read(int index, int length)
	{
		if (length <= 0)
		{
			return [];
		}

		VerifyRange(index, length);

		var buffer = new GapBuffer<T>();

		for (var i = 0; i < length; i++)
		{
			if ((index + i) >= Count)
			{
				break;
			}

			var item = this[index + i];
			buffer.Add(item);
		}

		return buffer.ToArray();
	}

	/// <inheritdoc />
	public int Read(int index, T[] buffer, int bufferIndex, int length)
	{
		VerifyRange(index);
		buffer.ValidRange(bufferIndex, length);

		var response = 0;
		var end = index + length;

		for (var i = index; i < end; i++)
		{
			if (i >= Count)
			{
				break;
			}

			var item = this[i];
			buffer[bufferIndex + response++] = item;
		}

		return response;
	}

	/// <inheritdoc />
	public abstract bool Remove(T item);

	/// <inheritdoc />
	public abstract void RemoveAt(int index);

	/// <inheritdoc />
	public abstract void RemoveRange(int index, int length);

	/// <summary>
	/// Replace a section of the buffer.
	/// </summary>
	/// <param name="index"> The start index to read from. </param>
	/// <param name="length"> The length of text to read. </param>
	/// <param name="values"> The values to replace with. </param>
	public virtual void Replace(int index, int length, IEnumerable<T> values)
	{
		var array = values.ToArray();
		Replace(index, length, array, 0, array.Length);
	}

	/// <summary>
	/// Replace a section of the buffer.
	/// </summary>
	/// <param name="index"> The start index to read from. </param>
	/// <param name="length"> The length of text to read. </param>
	/// <param name="values"> The values to replace with. </param>
	/// <param name="valueIndex"> The index in the values to start. </param>
	/// <param name="valueLength"> The length of values to replace with. </param>
	public virtual void Replace(int index, int length, T[] values, int valueIndex, int valueLength)
	{
		RemoveRange(index, length);
		Insert(index, values);
	}

	/// <summary>
	/// </summary>
	/// <param name="value"> </param>
	public virtual void Reset(params T[] value)
	{
		Replace(0, Count, value);
	}

	/// <summary>
	/// Throws ArgumentOutOfRangeException if the index is out of bounds.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void VerifyRange(int index)
	{
		if (Count <= 0)
		{
			return;
		}

		if ((index < 0) || (index >= Count))
		{
			throw new IndexOutOfRangeException(Babel.Tower[BabelKeys.IndexOutOfRange]);
		}
	}

	/// <summary>
	/// Check index and length to ensure it is within bounds of the buffer.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void VerifyRange(int index, int length)
	{
		if (Count <= 0)
		{
			return;
		}

		if ((index < 0) || (index >= Count))
		{
			throw new IndexOutOfRangeException(Babel.Tower[BabelKeys.IndexOutOfRange]);
		}

		var end = index + length;
		if ((end < 0) || (end > Count))
		{
			throw new IndexOutOfRangeException(Babel.Tower[BabelKeys.IndexAndLengthOutOfRange]);
		}
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}

/// <summary>
/// Represents a Buffer of values.
/// </summary>
public interface IBuffer<T> : IList<T>
{
	#region Methods

	/// <summary>
	/// Append the values to the buffer.
	/// </summary>
	/// <param name="items"> The values to append. </param>
	void Add(IEnumerable<T> items);

	/// <summary>
	/// Gets the index of the first occurrence the specified item.
	/// </summary>
	/// <param name="item"> Item to search for. </param>
	/// <param name="index"> The start index to read from. </param>
	/// <param name="length"> The length of text to read. </param>
	/// <returns> The first index where the item was found; or -1 if no occurrence was found. </returns>
	int IndexOf(T item, int index, int length);

	/// <summary>
	/// Gets the index of the first occurrence of any value in the specified array.
	/// </summary>
	/// <param name="anyOf"> The values to search for </param>
	/// <returns> The first index where any value was found otherwise -1 if not found. </returns>
	int IndexOfAny(T[] anyOf);

	/// <summary>
	/// Gets the index of the first occurrence of any value in the specified array.
	/// </summary>
	/// <param name="anyOf"> The values to search for </param>
	/// <param name="index"> The index to start at. </param>
	/// <param name="length"> Length of the area to search. </param>
	/// <returns> The first index where any value was found otherwise -1 if not found. </returns>
	int IndexOfAny(T[] anyOf, int index, int length);

	/// <summary>
	/// Insert a set of values into the builder.
	/// </summary>
	/// <param name="index"> The index to insert into. </param>
	/// <param name="values"> The values to insert. </param>
	void Insert(int index, IEnumerable<T> values);

	/// <summary>
	/// Inserts multiple items from a subset of items into the collection.
	/// </summary>
	void Insert(int index, T[] values, int valueIndex, int valueLength);

	/// <summary>
	/// Gets a range of values from the buffer.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <param name="length"> The number of values to attempt to read. </param>
	/// <returns> The read buffer. </returns>
	T[] Read(int index, int length);

	/// <summary>
	/// Gets a range of values from the buffer.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <param name="buffer"> The buffer to write to. </param>
	/// <param name="bufferIndex"> The buffer index to start at. </param>
	/// <param name="length"> The number of values to attempt to read. </param>
	/// <returns> The amount of items read into the buffer. </returns>
	public int Read(int index, T[] buffer, int bufferIndex, int length);

	/// <summary>
	/// Remove a range of items from the buffer.
	/// </summary>
	void RemoveRange(int index, int length);

	/// <summary>
	/// Check index to ensure it is within bounds of the buffer.
	/// </summary>
	void VerifyRange(int index);

	/// <summary>
	/// Check index and length to ensure it is within bounds of the buffer.
	/// </summary>
	void VerifyRange(int index, int length);

	#endregion
}