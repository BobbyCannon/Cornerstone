#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// A readonly filter proxy for a speedy list.
/// </summary>
public partial class FilteredPresentationList<T>
	: ReaderWriterLockProxy, IPresentationList<T>, IPresentationList
{
	#region Fields

	private readonly PresentationList<T> _filteredList;
	private readonly PresentationList<T> _list;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	/// <param name="list"> The unfiltered speedy list. </param>
	/// <param name="filterExpression"> The expression for filtering. </param>
	/// <param name="orderBy"> </param>
	public FilteredPresentationList(PresentationList<T> list, Func<T, bool> filterExpression, OrderBy<T>[] orderBy = null) : base(null)
	{
		_list = list;
		_filteredList = new PresentationList<T>(this, list.GetDispatcher(), orderBy ?? list.OrderBy)
		{
			FilterCheck = filterExpression
		};

		_filteredList.Load(_list);
		_list.ListUpdated += ListOnListUpdated;
		_list.PropertyChanged += ListOnPropertyChanged;
		_filteredList.ListUpdated += FilteredListOnListUpdated;
		_filteredList.CollectionChanged += FilteredListOnCollectionChanged;
		_filteredList.PropertyChanged += FilteredListOnPropertyChanged;
	}

	#endregion

	#region Properties

	/// <inheritdoc cref="IList" />
	public int Count => _filteredList.Count;

	/// <inheritdoc />
	public IEqualityComparer<T> DistinctCheck { get; set; }

	/// <summary>
	/// An optional filter to restrict the collection.
	/// </summary>
	public Func<T, bool> FilterCheck
	{
		get => _filteredList.FilterCheck;
		set => _filteredList.FilterCheck = value;
	}

	/// <summary>
	/// True if the list is currently filtering items.
	/// </summary>
	public bool IsFiltering => _filteredList.IsFiltering;

	/// <inheritdoc cref="IPresentationList" />
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

	public bool IsSynchronized => _filteredList.IsSynchronized;

	public T this[int index]
	{
		get => _filteredList[index];
		set => throw new NotSupportedException();
	}

	public OrderBy<T>[] OrderBy
	{
		get => _filteredList.OrderBy;
		set => throw new NotSupportedException();
	}

	public object SyncRoot => _filteredList.SyncRoot;

	object IList.this[int index]
	{
		get => this[index];
		set => throw new NotSupportedException();
	}

	#endregion

	#region Methods

	public void Add(T item)
	{
		_filteredList.Add(item);
	}

	public void Clear()
	{
		_filteredList.Clear();
	}

	public bool Contains(T item)
	{
		return _filteredList.Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		_filteredList.CopyTo(array, arrayIndex);
	}

	public IDispatcher GetDispatcher()
	{
		return _list.GetDispatcher();
	}

	public IEnumerator<T> GetEnumerator()
	{
		var list = _filteredList.ToList();
		return list.GetEnumerator();
	}

	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return _filteredList is ITrackPropertyChanges trackPropertyChanges
			&& trackPropertyChanges.HasChanges(settings);
	}

	public int IndexOf(T item)
	{
		return _filteredList.IndexOf(item);
	}

	public int IndexOf(Func<T, bool> predicate)
	{
		return _filteredList.IndexOf(predicate);
	}

	public void Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	public void Load(params T[] items)
	{
		_filteredList.Load(items);
	}

	public void Load(IEnumerable items)
	{
		_filteredList.Load(items);
	}

	public void Move(int oldIndex, int newIndex)
	{
		_filteredList.Move(oldIndex, newIndex);
	}

	public void ProcessThenOrder(Action process)
	{
		_filteredList.ProcessThenOrder(process);
	}

	public void RefreshFilter()
	{
		_filteredList.RefreshFilter();
	}

	public void RefreshOrder(bool force = false)
	{
		_filteredList.RefreshOrder(force);
	}

	public bool Remove(T item)
	{
		return _filteredList.Remove(item);
	}

	public void RemoveAt(int index)
	{
		var item = this[index];
		_filteredList.Remove(item);
	}

	public override void ResetHasChanges()
	{
		_filteredList.ResetHasChanges();
		base.ResetHasChanges();
	}

	public bool ShouldOrder()
	{
		return _filteredList.ShouldOrder();
	}

	public void Swap(int firstIndex, int secondIndex)
	{
		throw new NotSupportedException();
	}

	int IList.Add(object item)
	{
		return ((IList) _filteredList).Add(item);
	}

	bool IList.Contains(object item)
	{
		return Contains((T) item);
	}

	void ICollection.CopyTo(Array array, int arrayIndex)
	{
		if (_filteredList is IList list)
		{
			list.CopyTo(array, arrayIndex);
		}
	}

	private void FilteredListOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		NotifyComputedPropertyChanged(nameof(Count));
		CollectionChanged?.Invoke(this, e);
	}

	private void FilteredListOnListUpdated(object sender, PresentationListUpdatedEventArg<T> e)
	{
		NotifyComputedPropertyChanged(nameof(Count));
		ListUpdated?.Invoke(this, e);
	}

	private void FilteredListOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		// Pass all property changes to the proxy.
		NotifyComputedPropertyChanged(e.PropertyName);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	int IList.IndexOf(object item)
	{
		if (item is not T value)
		{
			return -1;
		}

		return _filteredList.IndexOf(value);
	}

	void IList.Insert(int index, object item)
	{
		throw new NotSupportedException();
	}

	private void ListOnListUpdated(object sender, PresentationListUpdatedEventArg<T> e)
	{
		if (_list.IsLoading && (e.Removed == null))
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

	public event EventHandler<PresentationListUpdatedEventArg<T>> ListUpdated;

	#endregion
}