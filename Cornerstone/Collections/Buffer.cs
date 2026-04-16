#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Collections;

public abstract class Buffer<T> : IBuffer<T>
{
	#region Properties

	public abstract int Count { get; }

	public abstract bool IsReadOnly { get; }

	public abstract T this[int index] { get; set; }

	#endregion

	#region Methods

	public virtual void Add(T item)
	{
		Add([item]);
	}

	public void Add(IEnumerable<T> items)
	{
		// Default slow path: element-by-element (override to optimize)
		foreach (var item in items)
		{
			Add(item);
		}
	}

	[OverloadResolutionPriority(1)]
	public void Add(ReadOnlySpan<T> items)
	{
		if (items.IsEmpty)
		{
			return;
		}

		Add(items, 0, items.Length);
	}

	public abstract void Add(ReadOnlySpan<T> items, int index, int length);

	public abstract void Clear();

	public abstract bool Contains(T item);

	public abstract void CopyTo(T[] array, int arrayIndex);

	public abstract IEnumerator<T> GetEnumerator();

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

	public abstract int IndexOf(T item, int index, int length);

	public abstract void Insert(int index, T item);

	public abstract T[] Read(int index, int length);

	public abstract bool Remove(T item);

	public abstract void RemoveAt(int index);

	public void RemoveAt(int index, int length)
	{
		for (var i = 0; i < length; i++)
		{
			RemoveAt(index);
		}
	}

	public void Reset(ReadOnlySpan<T> items)
	{
		Clear();
		Add(items);
	}

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
	/// AppendValue the values to the buffer.
	/// </summary>
	/// <param name="items"> The values to append. </param>
	void Add(IEnumerable<T> items);

	/// <summary>
	/// Appends a single item to the end of the buffer.
	/// </summary>
	/// <param name="items"> The items to append. </param>
	void Add(ReadOnlySpan<T> items);

	/// <summary>
	/// Gets the index of the first occurrence the specified item.
	/// </summary>
	/// <param name="item"> Item to search for. </param>
	/// <param name="index"> The start index to read from. </param>
	/// <param name="length"> The length of text to read. </param>
	/// <returns> The first index where the item was found; or -1 if no occurrence was found. </returns>
	int IndexOf(T item, int index, int length);

	/// <summary>
	/// Gets a range of values from the buffer.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <param name="length"> The number of values to attempt to read. </param>
	/// <returns> The read buffer. </returns>
	T[] Read(int index, int length);

	/// <summary>
	/// Clears then adds the items to the buffer.
	/// </summary>
	/// <param name="items"> The items to append. </param>
	void Reset(ReadOnlySpan<T> items);

	#endregion
}