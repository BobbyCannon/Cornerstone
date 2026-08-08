#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// A readonly proxy for a presentation list.
/// </summary>
public partial class ReadOnlyPresentationList<T> : ReaderWriterLockProxy, IPresentationList<T>, IPresentationList
{
	#region Constructors

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	public ReadOnlyPresentationList(PresentationList<T> list) : base(list)
	{
		List = list;
		List.ListUpdated += ListOnListUpdated;
		List.CollectionChanged += ListOnCollectionChanged;
		List.PropertyChanged += ListOnPropertyChanged;
	}

	#endregion

	#region Properties

	/// <inheritdoc cref="IList" />
	public int Count => List.Count;

	public IEqualityComparer<T> DistinctCheck
	{
		get => List.DistinctCheck;
		set => throw new NotSupportedException();
	}

	public Func<T, bool> FilterCheck
	{
		get => List.FilterCheck;
		set => throw new NotSupportedException();
	}

	public bool IsFiltering => List.IsFiltering;

	/// <inheritdoc cref="IPresentationList" />
	public bool IsFixedSize => false;

	/// <summary>
	/// True if the list is currently loading items.
	/// </summary>
	public bool IsLoading => List.IsLoading;

	/// <summary>
	/// True if the list is in the process of ordering.
	/// </summary>
	public bool IsOrdering => List.IsOrdering;

	/// <inheritdoc cref="IList" />
	public bool IsReadOnly => true;

	public bool IsSynchronized => true;

	public T this[int index]
	{
		get => ((IList<T>) List)[index];
		set => throw new NotSupportedException();
	}

	public OrderBy<T>[] OrderBy
	{
		get => List.OrderBy;
		set => throw new NotSupportedException();
	}

	public object SyncRoot => List.SyncRoot;

	protected PresentationList<T> List { get; }

	object IList.this[int index]
	{
		get => this[index];
		set => throw new NotSupportedException();
	}

	#endregion

	#region Methods

	/// <inheritdoc cref="IList" />
	public virtual void Clear()
	{
		throw new NotSupportedException();
	}

	public bool Contains(T item)
	{
		return List.Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		List.CopyTo(array, arrayIndex);
	}

	public IDispatcher GetDispatcher()
	{
		return List.GetDispatcher();
	}

	public IEnumerator<T> GetEnumerator()
	{
		var list = List.ToList();
		return list.GetEnumerator();
	}

	public FilteredPresentationList<T> GetFilteredList(Func<T, bool> filter, OrderBy<T>[] orderBys)
	{
		var list = new FilteredPresentationList<T>(List, filter, orderBys);
		list.RefreshFilter();
		return list;
	}

	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return (List is ITrackPropertyChanges trackPropertyChanges
				&& trackPropertyChanges.HasChanges(settings))
			|| base.HasChanges(settings);
	}

	public int IndexOf(T item)
	{
		return List.IndexOf(item);
	}

	public int IndexOf(Func<T, bool> predicate)
	{
		return List.IndexOf(predicate);
	}

	public void Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	public void Load(params T[] items)
	{
		throw new NotSupportedException();
	}

	public void Load(IEnumerable list)
	{
		throw new NotSupportedException();
	}

	public void Move(int oldIndex, int newIndex)
	{
		throw new NotSupportedException();
	}

	public void ProcessThenOrder(Action process)
	{
		throw new NotSupportedException();
	}

	public void RefreshFilter()
	{
		List.RefreshFilter();
	}

	public void RefreshOrder(bool force = false)
	{
		List.RefreshOrder(force);
	}

	public virtual bool Remove(T item)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc cref="IList" />
	public void RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Determine if the list should order.
	/// </summary>
	/// <returns> True if the list should order or false otherwise. </returns>
	public bool ShouldOrder()
	{
		return List.ShouldOrder();
	}

	public void Swap(int firstIndex, int secondIndex)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	int IList.Add(object item)
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object item)
	{
		return Contains((T) item);
	}

	void ICollection.CopyTo(Array array, int arrayIndex)
	{
		if (List is IList list)
		{
			list.CopyTo(array, arrayIndex);
		}
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

		return List.IndexOf(value);
	}

	void IList.Insert(int index, object item)
	{
		throw new NotSupportedException();
	}

	private void ListOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		NotifyComputedPropertyChanged(nameof(Count));
		CollectionChanged?.Invoke(this, e);
	}

	private void ListOnListUpdated(object sender, PresentationListUpdatedEventArg<T> e)
	{
		NotifyComputedPropertyChanged(nameof(Count));
		ListUpdated?.Invoke(this, e);
	}

	private void ListOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		// Pass all property changes to the proxy.
		NotifyComputedPropertyChanged(e.PropertyName);
	}

	void IList.Remove(object item)
	{
		Remove((T) item);
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