#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// A readonly filter proxy for a speedy list.
/// </summary>
public class FilteredSpeedyList<T> : ReaderWriterLockBindable, ISpeedyList<T>, IList
{
	#region Fields

	private readonly SpeedyList<T> _filteredList;
	private readonly SpeedyList<T> _list;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	/// <param name="list"> The unfiltered speedy list. </param>
	/// <param name="filterExpression"> The expression for filtering. </param>
	public FilteredSpeedyList(SpeedyList<T> list, Func<T, bool> filterExpression)
		: base(null, list.GetDispatcher())
	{
		_list = list;
		_filteredList = new SpeedyList<T>(this, list.GetDispatcher(), list.OrderBy)
		{
			FilterCheck = filterExpression
		};

		WeakEventManager.AddSpeedyListUpdated<SpeedyList<T>, T, FilteredSpeedyList<T>>(_list, this, ListOnListUpdated);
		WeakEventManager.AddPropertyChanged(_list, this, ListOnPropertyChanged);
		WeakEventManager.AddSpeedyListUpdated<SpeedyList<T>, T, FilteredSpeedyList<T>>(_filteredList, this, FilteredListOnListUpdated);
		WeakEventManager.AddCollectionChanged(_filteredList, this, FilteredListOnCollectionChanged);
		WeakEventManager.AddPropertyChanged(_filteredList, this, FilteredListOnPropertyChanged);
	}

	#endregion

	#region Properties

	/// <inheritdoc cref="IList" />
	public int Count => _filteredList.Count;

	/// <summary>
	/// An optional filter to restrict the collection.
	/// </summary>
	public Func<T, bool> FilterCheck
	{
		get => _filteredList.FilterCheck;
		set => _filteredList.FilterCheck = value;
	}

	public OrderBy<T>[] OrderBy { get; set; }

	/// <summary>
	/// True if the list is currently filtering items.
	/// </summary>
	public bool IsFiltering => _filteredList.IsFiltering;

	/// <inheritdoc cref="ISpeedyList" />
	public bool IsFixedSize => _filteredList.IsFixedSize;

	/// <summary>
	/// True if the list is currently loading items.
	/// </summary>
	public bool IsLoading => _filteredList.IsLoading;

	/// <summary>
	/// True if the list is in the process of ordering.
	/// </summary>
	public bool IsOrdering => _filteredList.IsOrdering;

	/// <inheritdoc cref="IList" />
	public bool IsReadOnly => true;

	/// <inheritdoc />
	public bool IsSynchronized => _filteredList.IsSynchronized;

	/// <inheritdoc />
	[SuppressPropertyChangedWarnings]
	public T this[int index]
	{
		get => _filteredList[index];
		set => throw new NotSupportedException();
	}

	public object SyncRoot => _filteredList.SyncRoot;

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
	public void Clear()
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public bool Contains(T item)
	{
		return _filteredList.Contains(item);
	}

	/// <inheritdoc />
	public void CopyTo(T[] array, int arrayIndex)
	{
		_filteredList.CopyTo(array, arrayIndex);
	}

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
	{
		var list = _filteredList.ToList();
		return list.GetEnumerator();
	}

	/// <inheritdoc />
	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return _filteredList is ITrackPropertyChanges trackPropertyChanges
			&& trackPropertyChanges.HasChanges(settings);
	}

	/// <inheritdoc />
	public int IndexOf(T item)
	{
		return _filteredList.IndexOf(item);
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
	public void RefreshFilter()
	{
		_filteredList.RefreshFilter();
	}

	/// <inheritdoc />
	public void RefreshOrder()
	{
		_filteredList.RefreshOrder();
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
		return _filteredList.ShouldOrder();
	}

	void ISpeedyList.Add(object item)
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
		if (_filteredList is IList list)
		{
			list.CopyTo(array, arrayIndex);
		}
	}

	[SuppressPropertyChangedWarnings]
	private void FilteredListOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		OnPropertyChanged(nameof(Count));
		CollectionChanged?.Invoke(this, e);
	}

	private void FilteredListOnListUpdated(object sender, SpeedyListUpdatedEventArg<T> e)
	{
		OnPropertyChanged(nameof(Count));
		ListUpdated?.Invoke(this, e);
	}

	private void FilteredListOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		// Pass all property changes to the proxy.
		OnPropertyChanged(e.PropertyName);
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

		return _filteredList.IndexOf(value);
	}

	/// <inheritdoc />
	int IList.IndexOf(object item)
	{
		if (item is not T value)
		{
			return -1;
		}

		return _filteredList.IndexOf(value);
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

	private void ListOnListUpdated(object sender, SpeedyListUpdatedEventArg<T> e)
	{
		if (_list.IsLoading && e.Removed == null)
		{
			// We are loading so just add
			_filteredList.Load(e.Added);
		}
		else
		{
			e.Removed?.ForEach(x => _filteredList.Remove(x));
			e.Added?.ForEach(x => _filteredList.Add(x));
		}
	}

	private void ListOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(_list.OrderBy):
			{
				_filteredList.OrderBy = _list.OrderBy;
				break;
			}
		}
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