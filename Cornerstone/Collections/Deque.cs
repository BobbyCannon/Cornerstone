#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Double-ended queue.
/// </summary>
[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
internal sealed class Deque<T> : ICollection<T>
{
	#region Fields

	private T[] _arr;
	private int _head;
	private int _tail;

	#endregion

	#region Constructors

	public Deque()
	{
		_arr = [];
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public int Count { get; private set; }

	/// <summary>
	/// Gets/Sets an element inside the deque.
	/// </summary>
	public T this[int index]
	{
		get
		{
			ThrowUtil.CheckInRangeInclusive(index, "index", 0, Count - 1);
			return _arr[(_head + index) % _arr.Length];
		}
		set
		{
			ThrowUtil.CheckInRangeInclusive(index, "index", 0, Count - 1);
			_arr[(_head + index) % _arr.Length] = value;
		}
	}

	bool ICollection<T>.IsReadOnly => false;

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Clear()
	{
		_arr = [];
		_head = 0;
		_tail = 0;

		Count = 0;
	}

	/// <inheritdoc />
	public bool Contains(T item)
	{
		var comparer = System.Collections.Generic.EqualityComparer<T>.Default;
		foreach (var element in this)
		{
			if (comparer.Equals(item, element))
			{
				return true;
			}
		}
		return false;
	}

	/// <inheritdoc />
	public void CopyTo(T[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException(nameof(array));
		}
		if (_head < _tail)
		{
			Array.Copy(_arr, _head, array, arrayIndex, _tail - _head);
		}
		else
		{
			var num1 = _arr.Length - _head;
			Array.Copy(_arr, _head, array, arrayIndex, num1);
			Array.Copy(_arr, 0, array, arrayIndex + num1, _tail);
		}
	}

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
	{
		if (_head < _tail)
		{
			for (var i = _head; i < _tail; i++)
			{
				yield return _arr[i];
			}
		}
		else
		{
			for (var i = _head; i < _arr.Length; i++)
			{
				yield return _arr[i];
			}
			for (var i = 0; i < _tail; i++)
			{
				yield return _arr[i];
			}
		}
	}

	/// <summary>
	/// Pops an element from the end of the deque.
	/// </summary>
	public T PopEnd()
	{
		if (Count == 0)
		{
			throw new InvalidOperationException();
		}
		if (_tail == 0)
		{
			_tail = _arr.Length - 1;
		}
		else
		{
			_tail--;
		}
		var val = _arr[_tail];
		_arr[_tail] = default; // allow GC to collect the element
		Count--;
		return val;
	}

	/// <summary>
	/// Pops an element from the end of the deque.
	/// </summary>
	public T PopFront()
	{
		if (Count == 0)
		{
			throw new InvalidOperationException();
		}
		var val = _arr[_head];
		_arr[_head] = default; // allow GC to collect the element
		_head++;
		if (_head == _arr.Length)
		{
			_head = 0;
		}
		Count--;
		return val;
	}

	/// <summary>
	/// Adds an element to the end of the deque.
	/// </summary>
	[SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "PushBack")]
	public void PushEnd(T item)
	{
		if (Count == _arr.Length)
		{
			SetCapacity(Math.Max(4, _arr.Length * 2));
		}
		_arr[_tail++] = item;
		if (_tail == _arr.Length)
		{
			_tail = 0;
		}
		Count++;
	}

	/// <summary>
	/// Adds an element to the front of the deque.
	/// </summary>
	public void PushFront(T item)
	{
		if (Count == _arr.Length)
		{
			SetCapacity(Math.Max(4, _arr.Length * 2));
		}
		if (_head == 0)
		{
			_head = _arr.Length - 1;
		}
		else
		{
			_head--;
		}
		_arr[_head] = item;
		Count++;
	}

	void ICollection<T>.Add(T item)
	{
		PushEnd(item);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	private void SetCapacity(int capacity)
	{
		var newArr = new T[capacity];
		CopyTo(newArr, 0);
		_head = 0;
		_tail = Count == capacity ? 0 : Count;
		_arr = newArr;
	}

	#endregion
}