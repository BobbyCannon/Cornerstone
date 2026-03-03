#region References

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Models;

public abstract class ReadOnlyListBase<T> : IReadOnlyList<T>, IList
{
	#region Properties

	public abstract int Count { get; }
	public abstract T this[int index] { get; }

	bool IList.IsFixedSize => false;
	bool IList.IsReadOnly => true;
	bool ICollection.IsSynchronized => false;

	object IList.this[int index]
	{
		get => this[index];
		set => throw new NotSupportedException();
	}

	object ICollection.SyncRoot => this;

	#endregion

	#region Methods

	public abstract IEnumerator<T> GetEnumerator();

	int IList.Add(object value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object value)
	{
		return ((IList) this).IndexOf(value) != -1;
	}

	void ICollection.CopyTo(Array array, int index)
	{
		for (var i = 0; i < Count; ++i)
		{
			array.SetValue(this[i], i + index);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	int IList.IndexOf(object value)
	{
		for (var i = 0; i < Count; ++i)
		{
			if (Equals(this[i], value))
			{
				return i;
			}
		}

		return -1;
	}

	void IList.Insert(int index, object value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	#endregion
}