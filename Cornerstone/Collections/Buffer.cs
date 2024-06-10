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
	public abstract int Count { get; }

	/// <inheritdoc />
	public abstract bool IsReadOnly { get; }

	/// <inheritdoc />
	public abstract T this[int index] { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public abstract void Add(T item);

	/// <summary>
	/// Check index and length to ensure it is within bounds of the array.
	/// </summary>
	/// <param name="array"> The array to check. </param>
	/// <param name="index"> The index to start in the array. </param>
	/// <param name="length"> The length to the end index of the array. </param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BoundsCheckArray(T[] array, int index, int length)
	{
		if (array == null)
		{
			throw new ArgumentNullException(nameof(array));
		}

		if ((index < 0) || (index >= array.Length))
		{
			throw new IndexOutOfRangeException(Babel.Tower[BabelKeys.IndexOutOfRange]);
		}

		if ((length < 0) || ((index + length) > array.Length))
		{
			throw new IndexOutOfRangeException(Babel.Tower[BabelKeys.IndexAndLengthOutOfRange]);
		}
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
	/// <param name="index"> The start index to read from. </param>
	/// <param name="length"> The length of text to read. </param>
	/// <returns> The first index where the item was found; or -1 if no occurrence was found. </returns>
	public abstract int IndexOf(T item, int index, int length);

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
	public abstract void Insert(int index, T item);

	/// <summary>
	/// Inserts a set of items to the buffer at the specified index.
	/// </summary>
	/// <param name="index"> The zero-based index at which item(s) should be inserted. </param>
	/// <param name="items"> The items to be inserted. </param>
	public abstract void InsertRange(int index, T[] items);

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
	/// <param name="value"> The values to replace with. </param>
	public void Replace(int index, int length, params T[] value)
	{
		VerifyRange(index, length);
		RemoveRange(index, length);
		InsertRange(index, value);
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
	/// Check index and length to ensure it is within bounds of the array.
	/// </summary>
	/// <param name="array"> The array to check. </param>
	/// <param name="index"> The index to start in the array. </param>
	/// <param name="length"> The length to the end index of the array. </param>
	void BoundsCheckArray(T[] array, int index, int length);

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