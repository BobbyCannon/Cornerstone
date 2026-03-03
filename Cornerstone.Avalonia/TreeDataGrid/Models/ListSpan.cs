#region References

using System;
using System.Collections;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Models;

internal class ListSpan : IList
{
	#region Fields

	private readonly int _count;
	private readonly int _index;
	private readonly IList _items;

	#endregion

	#region Constructors

	public ListSpan(IList items, int index, int count)
	{
		_items = items;
		_index = index;
		_count = count;
	}

	#endregion

	#region Properties

	public object this[int index]
	{
		get
		{
			if (index >= _count)
			{
				throw new ArgumentOutOfRangeException();
			}
			return _items[_index + index];
		}
		set => throw new NotSupportedException();
	}

	int ICollection.Count => _count;

	bool IList.IsFixedSize => true;
	bool IList.IsReadOnly => true;
	bool ICollection.IsSynchronized => false;
	object ICollection.SyncRoot => this;

	#endregion

	#region Methods

	public IEnumerator GetEnumerator()
	{
		for (var i = 0; i < _count; ++i)
		{
			yield return _items[_index + i];
		}
	}

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
		throw new NotSupportedException();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		throw new NotSupportedException();
	}

	int IList.IndexOf(object value)
	{
		throw new NotSupportedException();
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