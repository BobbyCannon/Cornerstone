#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

	/// <inheritdoc />
	public abstract IEnumerator<T> GetEnumerator();

	/// <inheritdoc />
	public abstract int IndexOf(T item);

	/// <inheritdoc />
	public abstract void Insert(int index, T item);

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
	public abstract bool Remove(T item);

	/// <inheritdoc />
	public abstract void RemoveAt(int index);

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
	/// Check index to ensure it is within bounds of the buffer.
	/// </summary>
	void VerifyRange(int index);

	/// <summary>
	/// Check index and length to ensure it is within bounds of the buffer.
	/// </summary>
	void VerifyRange(int index, int length);

	#endregion
}