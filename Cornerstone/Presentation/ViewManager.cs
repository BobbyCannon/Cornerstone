#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Cornerstone.Collections;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Presentation;

public class ViewManager<T> : Notifiable, IList<T>, IList, INotifyCollectionChanged, IDisposable
{
	#region Fields

	private readonly SpeedyList<T> _list;

	#endregion

	#region Constructors

	public ViewManager(int initialCapacity = SpeedyList.DefaultCapacity, bool isLongLivedBuffer = false, bool clearOnCleanup = false)
	{
		_list = new SpeedyList<T>(initialCapacity, isLongLivedBuffer, clearOnCleanup);
	}

	#endregion

	#region Properties

	public int Count => _list.Count;

	public bool IsFixedSize => false;

	public bool IsReadOnly => false;

	public bool IsSynchronized => false;

	public T this[int index]
	{
		get => _list[index];
		set => _list[index] = value;
	}

	object IList.this[int index]
	{
		get => this[index];
		set => this[index] = (T) value!;
	}

	object ICollection.SyncRoot => this;

	#endregion

	#region Methods

	public void Add(T item)
	{
		_list.Add(item);
		OnCollectionChanged(NotifyCollectionChangedAction.Add, item, Count - 1);
		OnPropertyChanged(nameof(Count));
	}

	public int Add(object value)
	{
		if (value is T item)
		{
			Add(item);
			return Count - 1;
		}
		throw new ArgumentException($"Value must be of type {typeof(T).Name}", nameof(value));
	}

	public ReadOnlySpan<T> AsSpan()
	{
		return _list.AsSpan();
	}

	public virtual void Clear()
	{
		_list.Clear();
		OnCollectionReset();
		OnPropertyChanged(nameof(Count));
	}

	public bool Contains(T item)
	{
		return _list.Contains(item);
	}

	public bool Contains(object value)
	{
		return value is T item && Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		_list.CopyTo(array, arrayIndex);
	}

	public void CopyTo(Array array, int index)
	{
		if (array is T[] typedArray)
		{
			CopyTo(typedArray, index);
		}
		else
		{
			throw new ArgumentException("Invalid array type", nameof(array));
		}
	}

	public void Dispose()
	{
	}

	public IEnumerator<T> GetEnumerator()
	{
		return ((IEnumerable<T>) _list).GetEnumerator();
	}

	public int IndexOf(T item)
	{
		return _list.IndexOf(item);
	}

	public int IndexOf(object value)
	{
		return value is T item ? IndexOf(item) : -1;
	}

	public void Insert(int index, T item)
	{
		_list.Insert(index, item);
		OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
		OnPropertyChanged(nameof(Count));
	}

	public void Insert(int index, object value)
	{
		if (value is T item)
		{
			Insert(index, item);
		}
		else
		{
			throw new ArgumentException($"Value must be of type {typeof(T).Name}", nameof(value));
		}
	}

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

	public void Remove(object value)
	{
		if (value is T item)
		{
			Remove(item);
		}
	}

	public void RemoveAt(int index)
	{
		if ((index < 0) || (index >= Count))
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		var removedItem = this[index];

		_list.RemoveAt(index);

		OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
		OnPropertyChanged(nameof(Count));
	}

	protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	{
		CollectionChanged?.Invoke(this, e);
	}

	protected void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
	{
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
	}

	protected void OnCollectionReset()
	{
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion

	#region Events

	public event NotifyCollectionChangedEventHandler CollectionChanged;

	#endregion
}