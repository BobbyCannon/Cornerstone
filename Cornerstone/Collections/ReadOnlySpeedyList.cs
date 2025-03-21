#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// A readonly proxy for a speedy list.
/// </summary>
public class ReadOnlySpeedyList<T> : ReaderWriterLockBindable, ISpeedyList<T>, IList
{
	#region Fields

	private readonly SpeedyList<T> _list;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	public ReadOnlySpeedyList(SpeedyList<T> list)
		: base(list, list.GetDispatcher())
	{
		_list = list;

		var weakEventManager = Platform.GetInstance<WeakEventManager>();
		weakEventManager.AddSpeedyListUpdated<SpeedyList<T>, T, ReadOnlySpeedyList<T>>(_list, this, ListOnListUpdated);
		weakEventManager.AddCollectionChanged(_list, this, ListOnCollectionChanged);
			weakEventManager.AddPropertyChanged(_list, this, ListOnPropertyChanged);
	}

	#endregion

	#region Properties

	/// <inheritdoc cref="IList" />
	public int Count => _list.Count;

	public bool IsFiltering => _list.IsFiltering;

	/// <inheritdoc cref="ISpeedyList" />
	public bool IsFixedSize => false;

	/// <summary>
	/// True if the list is currently loading items.
	/// </summary>
	public bool IsLoading => _list.IsLoading;

	/// <summary>
	/// True if the list is in the process of ordering.
	/// </summary>
	public bool IsOrdering => _list.IsOrdering;

	/// <inheritdoc cref="IList" />
	public bool IsReadOnly => true;

	/// <inheritdoc />
	public bool IsSynchronized => true;

	/// <inheritdoc />
	[SuppressPropertyChangedWarnings]
	public T this[int index]
	{
		get => ((IList<T>) _list)[index];
		set => throw new NotSupportedException();
	}

	public object SyncRoot => _list.SyncRoot;

	[SuppressPropertyChangedWarnings]
	object ISpeedyList.this[int index]
	{
		get => this[index];
		set => throw new NotSupportedException();
	}

	/// <inheritdoc />
	[SuppressPropertyChangedWarnings]
	object IList.this[int index]
	{
		get => this[index];
		set => throw new NotSupportedException();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Add an item to the list.
	/// </summary>
	/// <param name="item"> The item to add. </param>
	/// <returns> The index where the item exist after add. </returns>
	public void Add(T item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc cref="IList" />
	public virtual void Clear()
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public bool Contains(T item)
	{
		return _list.Contains(item);
	}

	/// <inheritdoc />
	public void CopyTo(T[] array, int arrayIndex)
	{
		_list.CopyTo(array, arrayIndex);
	}

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
	{
		var list = _list.ToList();
		return list.GetEnumerator();
	}

	/// <inheritdoc />
	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return _list is ITrackPropertyChanges trackPropertyChanges
			&& trackPropertyChanges.HasChanges(settings);
	}

	/// <inheritdoc />
	public int IndexOf(T item)
	{
		return _list.IndexOf(item);
	}

	/// <inheritdoc />
	public void Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public void Load(params T[] items)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public void Move(int oldIndex, int newIndex)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public bool Remove(T item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc cref="IList" />
	public void RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public override void ResetHasChanges()
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Determine if the list should order.
	/// </summary>
	/// <returns> True if the list should order or false otherwise. </returns>
	public bool ShouldOrder()
	{
		return _list.ShouldOrder();
	}

	void ISpeedyList.Add(object item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	int IList.Add(object item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	bool IList.Contains(object item)
	{
		return Contains((T) item);
	}

	bool ISpeedyList.Contains(object item)
	{
		return Contains((T) item);
	}

	/// <inheritdoc />
	void ICollection.CopyTo(Array array, int arrayIndex)
	{
		if (_list is IList list)
		{
			list.CopyTo(array, arrayIndex);
		}
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	int ISpeedyList.IndexOf(object item)
	{
		if (item is not T value)
		{
			return -1;
		}

		return _list.IndexOf(value);
	}

	/// <inheritdoc />
	int IList.IndexOf(object item)
	{
		if (item is not T value)
		{
			return -1;
		}

		return _list.IndexOf(value);
	}

	void ISpeedyList.Insert(int index, object item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	void IList.Insert(int index, object item)
	{
		throw new NotSupportedException();
	}

	[SuppressPropertyChangedWarnings]
	private void ListOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		OnPropertyChanged(nameof(Count));
		CollectionChanged?.Invoke(this, e);
	}

	private void ListOnListUpdated(object sender, SpeedyListUpdatedEventArg<T> e)
	{
		OnPropertyChanged(nameof(Count));
		ListUpdated?.Invoke(this, e);
	}

	private void ListOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		// Pass all property changes to the proxy.
		OnPropertyChanged(e.PropertyName);
	}

	void ISpeedyList.Remove(object item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	void IList.Remove(object item)
	{
		throw new NotSupportedException();
	}

	#endregion

	#region Events

	/// <summary>
	/// Used for notifying presentation layers the collection changed.
	/// Note: There is a few gotchas with CollectionChanged. Not all change
	/// notifications provide the changes with the notification. Ex. When
	/// the list is cleared the items are not provided but rather it's just
	/// a Reset event. This is due to limitations with the
	/// <see cref="INotifyCollectionChanged" /> interface. See links in the
	/// class description.
	/// </summary>
	public event NotifyCollectionChangedEventHandler CollectionChanged;

	/// <inheritdoc />
	public event EventHandler<SpeedyListUpdatedEventArg<T>> ListUpdated;

	#endregion
}