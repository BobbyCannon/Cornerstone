#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// A thread-safe, dispatch safe, limitable, orderable, filterable, and observable list.
/// Dispatch safe, limit, orderable, and filterable settings are optional.
/// </summary>
/// <typeparam name="T"> The type of items in the list. </typeparam>
//[SourceReflection]
public partial class SpeedyList<T> : ReaderWriterLockBindable, ISpeedyList<T>, ISpeedyList
{
	#region Fields

	private readonly List<T> _activeItems;
	private readonly List<T> _allItems;
	private bool _hasChanges;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	public SpeedyList() : this(Array.Empty<T>())
	{
	}

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	/// <param name="items"> An optional set. </param>
	public SpeedyList(params T[] items) : this(null, null, null, items)
	{
	}

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	/// <param name="orderBy"> The optional set of order by settings. </param>
	public SpeedyList(OrderBy<T>[] orderBy) : this(null, null, orderBy)
	{
	}

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public SpeedyList(IDispatcher dispatcher) : this(null, dispatcher, [])
	{
	}

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	/// <param name="items"> An optional set. </param>
	public SpeedyList(IDispatcher dispatcher, params T[] items) : this(null, dispatcher, [], items)
	{
	}

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	/// <param name="orderBy"> The optional set of order by settings. </param>
	public SpeedyList(IDispatcher dispatcher, params OrderBy<T>[] orderBy)
		: this(null, dispatcher, orderBy)
	{
	}

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	/// <param name="orderBy"> The optional set of order by settings. </param>
	/// <param name="items"> An optional set. </param>
	public SpeedyList(IDispatcher dispatcher, OrderBy<T>[] orderBy, params T[] items)
		: this(null, dispatcher, orderBy, items)
	{
	}

	/// <summary>
	/// Create an instance of the list.
	/// </summary>
	/// <param name="readerWriterLock"> An optional lock. Defaults to <see cref="ReaderWriterLockTiny" /> if not provided. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	/// <param name="orderBy"> The optional set of order by settings. </param>
	/// <param name="items"> An optional set. </param>
	public SpeedyList(IReaderWriterLock readerWriterLock, IDispatcher dispatcher, OrderBy<T>[] orderBy, params T[] items)
		: base(readerWriterLock, dispatcher)
	{
		_activeItems = [];
		_allItems = [];

		DistinctCheck = null;
		Limit = int.MaxValue;
		OrderBy = orderBy;
		IsOrdering = false;
		SyncRoot = new object();

		if (items?.Length > 0)
		{
			Load(items);
		}

		_hasChanges = false;
	}

	#endregion

	#region Properties

	/// <inheritdoc cref="ISpeedyList" />
	public int Count => _activeItems.Count;

	/// <summary>
	/// An optional comparer to use if you want a distinct list.
	/// </summary>
	[Notify]
	public partial IEqualityComparer<T> DistinctCheck { get; set; }

	/// <summary>
	/// An optional filter to restrict the collection.
	/// </summary>
	[Notify]
	public partial Func<T, bool> FilterCheck { get; set; }

	/// <summary>
	/// True if the list is currently filtering items.
	/// </summary>
	[Notify]
	public partial bool IsFiltering { get; private set; }

	/// <inheritdoc cref="ISpeedyList" />
	public bool IsFixedSize => false;

	/// <summary>
	/// True if the list is currently loading items.
	/// </summary>
	[Notify]
	public partial bool IsLoading { get; private set; }

	/// <summary>
	/// True if the list is in the process of ordering.
	/// </summary>
	[Notify]
	public partial bool IsOrdering { get; protected set; }

	/// <inheritdoc cref="ISpeedyList" />
	public bool IsReadOnly => false;

	/// <inheritdoc />
	public bool IsSynchronized => true;

	/// <inheritdoc />
	public T this[int index]
	{
		get
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			try
			{
				EnterReadLock();

				if (index >= _activeItems.Count)
				{
					throw new ArgumentOutOfRangeException(nameof(index));
				}

				return _activeItems[index];
			}
			finally
			{
				ExitReadLock();
			}
		}
		set
		{
			T oldItem;

			try
			{
				EnterWriteLock();

				oldItem = _activeItems[index];
				if (oldItem.Equals(value))
				{
					return;
				}

				_activeItems[index] = value;

				var sourceIndex = _allItems.IndexOf(oldItem);
				if (sourceIndex >= 0)
				{
					_allItems[sourceIndex] = value;
				}
			}
			finally
			{
				ExitWriteLock();
			}

			var filtered = InternalFilter();

			this.Dispatch(() =>
			{
				OnListUpdated([value], [oldItem]);

				if (!filtered)
				{
					// Only call replace if not filter, filter will fire a [Reset].
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem, index));
				}
			});
		}
	}

	/// <summary>
	/// The maximum limit for this list.
	/// </summary>
	[Notify]
	public partial int Limit { get; set; }

	/// <summary>
	/// The expression to order this collection by.
	/// </summary>
	[Notify]
	public partial OrderBy<T>[] OrderBy { get; set; }

	/// <summary>
	/// Flag to track pausing of ordering.
	/// </summary>
	[Notify]
	public partial bool PauseOrdering { get; private set; }

	/// <inheritdoc cref="ISpeedyList" />
	public object SyncRoot { get; }

	object IList.this[int index]
	{
		get => this[index];
		set => this[index] = (T) value;
	}

	#endregion

	#region Methods

	/// <summary>
	/// Add an item to the list.
	/// </summary>
	/// <param name="item"> The item to add. </param>
	/// <returns> The index where the item exist after add. </returns>
	public virtual T Add(T item)
	{
		AddWithLock(item);
		return item;
	}

	/// <inheritdoc cref="IList" />
	public virtual void Clear()
	{
		if ((_activeItems.Count <= 0) && (_allItems.Count <= 0))
		{
			return;
		}

		T[] removedItems;

		try
		{
			EnterWriteLock();

			removedItems = _activeItems.ToArray();

			_activeItems.Clear();
			_allItems.Clear();
		}
		finally
		{
			ExitWriteLock();
		}

		this.Dispatch(() =>
		{
			OnListUpdated(null, removedItems);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			OnPropertyChanged(nameof(Count));
		});
	}

	public bool Contains(T item)
	{
		try
		{
			EnterReadLock();
			return InternalContains(item);
		}
		finally
		{
			ExitReadLock();
		}
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		try
		{
			EnterReadLock();
			_activeItems.CopyTo(array, arrayIndex);
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <summary>
	/// Get the first item in the list or default item.
	/// </summary>
	/// <param name="predicate"> The predicate filter. </param>
	/// <returns> The first item or default. </returns>
	public T Find(Func<T, bool> predicate)
	{
		return FirstOrDefault(predicate);
	}

	/// <summary>
	/// Get the first item in the list.
	/// </summary>
	/// <returns> The first item. </returns>
	public T First()
	{
		try
		{
			EnterReadLock();
			return _activeItems.First();
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <summary>
	/// Get the first item in the list.
	/// </summary>
	/// <param name="predicate"> The predicate filter. </param>
	/// <returns> The first item. </returns>
	public T First(Func<T, bool> predicate)
	{
		try
		{
			EnterReadLock();
			return _activeItems.First(predicate);
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <summary>
	/// Get the first item in the list or default item.
	/// </summary>
	/// <returns> The first item or default. </returns>
	public T FirstOrDefault()
	{
		try
		{
			EnterReadLock();
			return _activeItems.FirstOrDefault();
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <summary>
	/// Get the first item in the list that matches the predicate or default item.
	/// </summary>
	/// <param name="predicate"> The predicate filter. </param>
	/// <returns> The first item or default. </returns>
	public virtual T FirstOrDefault(Func<T, bool> predicate)
	{
		try
		{
			EnterReadLock();
			return _activeItems.FirstOrDefault(predicate);
		}
		finally
		{
			ExitReadLock();
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		T[] snapshot;

		try
		{
			EnterReadLock();
			snapshot = new T[_activeItems.Count];
			_activeItems.CopyTo(snapshot, 0);
		}
		finally
		{
			ExitReadLock();
		}

		return ((IEnumerable<T>) snapshot).GetEnumerator();
	}

	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		if (_hasChanges)
		{
			return true;
		}

		return typeof(T).ImplementsType<ITrackPropertyChanges>()
			&& this.Any(x => (x as ITrackPropertyChanges)?.HasChanges() ?? false);
	}

	public int IndexOf(T item)
	{
		try
		{
			EnterReadLock();
			return InternalIndexOf(item);
		}
		finally
		{
			ExitReadLock();
		}
	}

	public int IndexOf(Func<T, bool> predicate)
	{
		try
		{
			EnterReadLock();
			return InternalIndexOf(_activeItems, predicate);
		}
		finally
		{
			ExitReadLock();
		}
	}

	public void Insert(int index, T item)
	{
		if (ShouldOrder())
		{
			// Just add because the list is an ordered list.
			Add(item);
			return;
		}

		try
		{
			EnterUpgradeableReadLock();

			if (InternalIndexOf(_allItems, item, DistinctCheck) < 0)
			{
				_allItems.Add(item);
			}

			if ((FilterCheck == null) || FilterCheck(item))
			{
				if (InternalDistinct(item, out var foundIndex))
				{
					if (foundIndex != index)
					{
						InternalMove(foundIndex, index);
					}
					return;
				}

				if (!InternalInsert(index, item))
				{
					return;
				}
			}

			InternalEnforceLimit(false);
		}
		finally
		{
			ExitUpgradeableReadLock();
		}
	}

	/// <inheritdoc cref="ISpeedyList" />
	public void Insert(int index, object item)
	{
		Insert(index, (T) item);
	}

	/// <summary>
	/// Get the last item in the list or default item.
	/// </summary>
	/// <returns> The last item or default. </returns>
	public T Last()
	{
		try
		{
			EnterReadLock();
			return _activeItems.Last();
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <summary>
	/// Get the last item in the list or default item.
	/// </summary>
	/// <param name="predicate"> The predicate filter. </param>
	/// <returns> The last item or default. </returns>
	public T Last(Func<T, bool> predicate)
	{
		try
		{
			EnterReadLock();
			return _activeItems.Last(predicate);
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <summary>
	/// Get the last item in the list or default item.
	/// </summary>
	/// <returns> The last item or default. </returns>
	public T LastOrDefault()
	{
		try
		{
			EnterReadLock();
			return _activeItems.LastOrDefault();
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <summary>
	/// Get the last item in the list or default item.
	/// </summary>
	/// <param name="predicate"> The predicate filter. </param>
	/// <returns> The last item or default. </returns>
	public T LastOrDefault(Func<T, bool> predicate)
	{
		try
		{
			EnterReadLock();
			return _activeItems.LastOrDefault(predicate);
		}
		finally
		{
			ExitReadLock();
		}
	}

	public void Load(IEnumerable list)
	{
		var items = list.Cast<T>().ToArray();
		Load(items);
	}

	/// <summary>
	/// Loads the items into the list. All existing items will be cleared.
	/// </summary>
	/// <param name="items"> The items to be loaded. </param>
	public void Load(IEnumerable<T> items)
	{
		Load(items.ToArray());
	}

	/// <summary>
	/// Loads the items into the list. All existing items will be cleared.
	/// </summary>
	/// <param name="items"> The items to be loaded. </param>
	public void Load(params T[] items)
	{
		InternalLoad(items);
	}

	/// <summary>
	/// Moves an item to a new index.
	/// </summary>
	/// <param name="oldIndex"> The index of the item to move. </param>
	/// <param name="newIndex"> The index to move the item to. </param>
	public void Move(int oldIndex, int newIndex)
	{
		if (OrderBy is { Length: > 0 })
		{
			// Do not move when ordering...
			return;
		}

		T item;

		try
		{
			EnterUpgradeableReadLock();
			item = this[oldIndex];

			EnterWriteLock();

			_activeItems.RemoveAt(oldIndex);
			_activeItems.Insert(newIndex, item);
		}
		finally
		{
			ExitWriteLock();
			ExitUpgradeableReadLock();
		}

		this.Dispatch(() =>
		{
			var e = new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Move,
				item,
				newIndex,
				oldIndex
			);
			OnCollectionChanged(e);
			OnPropertyChanged(nameof(Count));
		});
	}

	/// <summary>
	/// Process an action then order the collection.
	/// </summary>
	/// <param name="process"> The process to execute before ordering. </param>
	public void ProcessThenOrder(Action process)
	{
		ProcessThenOrder<object>(() =>
		{
			process();
			return null;
		});
	}

	/// <summary>
	/// Process an action then order, filter, event on the collection changes. ** see remarks **
	/// </summary>
	/// <param name="process"> The process to execute before ordering. </param>
	/// <typeparam name="T2"> The type of the item from the process. </typeparam>
	/// <returns> The items returned from the process. </returns>
	public T2 ProcessThenOrder<T2>(Func<T2> process)
	{
		// Check to see if we are already ordering
		if (IsOrdering || PauseOrdering)
		{
			// Do the processing
			return process();
		}

		try
		{
			// Disable ordering
			PauseOrdering = true;

			// Do the processing
			return process();
		}
		finally
		{
			// Re-enable ordering then order
			PauseOrdering = false;

			RefreshOrder();
			InternalEnforceLimit();
		}
	}

	/// <summary>
	/// Refresh the filter.
	/// </summary>
	public void RefreshFilter()
	{
		try
		{
			EnterUpgradeableReadLock();
			InternalFilter();
		}
		finally
		{
			ExitUpgradeableReadLock();
		}
	}

	public void RefreshOrder(bool force = false)
	{
		if (!ShouldOrder() && !force)
		{
			return;
		}

		try
		{
			EnterUpgradeableReadLock();
			InternalOrderWithoutLocking(force);
		}
		finally
		{
			ExitUpgradeableReadLock();
		}
	}

	public bool Remove(T item)
	{
		T removedItem;
		int index;

		try
		{
			EnterWriteLock();

			_allItems.Remove(item);

			index = InternalIndexOf(item);

			if (index < 0)
			{
				// item was filter and not active
				return false;
			}

			removedItem = _activeItems[index];

			_activeItems.RemoveAt(index);
		}
		finally
		{
			ExitWriteLock();
		}

		this.Dispatch(() =>
		{
			var e = new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Remove,
				removedItem,
				index
			);
			OnListUpdated([], [removedItem]);
			OnCollectionChanged(e);
			OnPropertyChanged(nameof(Count));
		});

		return true;
	}

	/// <summary>
	/// Remove all entries that match predicate
	/// </summary>
	/// <param name="predicate"> The predicate to find entries to remove. </param>
	public void Remove(Func<T, bool> predicate)
	{
		try
		{
			EnterUpgradeableReadLock();

			for (var i = _activeItems.Count - 1; i >= 0; i--)
			{
				if (predicate.Invoke(_activeItems[i]))
				{
					InternalRemoveAt(i);
				}
			}
		}
		finally
		{
			ExitUpgradeableReadLock();
		}
	}

	/// <inheritdoc cref="ISpeedyList" />
	public void Remove(object item)
	{
		Remove((T) item);
	}

	/// <inheritdoc cref="IList" />
	public void RemoveAt(int index)
	{
		InternalRemoveAt(index);
	}

	/// <summary>
	/// Remove all items from the collection.
	/// </summary>
	/// <param name="index"> The zero-based starting index of the range of items to remove. </param>
	/// <param name="length"> The number of items to remove. </param>
	public void RemoveRange(int index, int length)
	{
		T[] removed;

		try
		{
			EnterUpgradeableReadLock();

			var start = (index + length) - 1;
			if (start >= Count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			removed = new T[length];
			_activeItems.CopyTo(index, removed, 0, length);

			try
			{
				EnterWriteLock();

				_activeItems.RemoveRange(index, length);
			}
			finally
			{
				ExitWriteLock();
			}
		}
		finally
		{
			ExitUpgradeableReadLock();
		}

		this.Dispatch(() =>
		{
			OnListUpdated(null, removed);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, index));
			OnPropertyChanged(nameof(Count));
		});
	}

	public override void ResetHasChanges()
	{
		_hasChanges = false;

		if (!typeof(T).ImplementsType<ITrackPropertyChanges>())
		{
			return;
		}

		foreach (var item in this)
		{
			(item as ITrackPropertyChanges)?.ResetHasChanges();
		}
	}

	/// <summary>
	/// Determine if the list should order.
	/// </summary>
	/// <returns> True if the list should order or false otherwise. </returns>
	public virtual bool ShouldOrder()
	{
		return !IsLoading
			&& !PauseOrdering
			&& !IsOrdering
			&& (_activeItems.Count > 0)
			&& OrderBy is { Length: > 0 };
	}

	public void Swap(int firstIndex, int secondIndex)
	{
		if (OrderBy is { Length: > 0 }
			|| (firstIndex == secondIndex))
		{
			// Do not move when ordering...
			return;
		}

		var minIndex = Math.Min(firstIndex, secondIndex);
		var maxIndex = Math.Max(firstIndex, secondIndex);

		Move(maxIndex, minIndex);
		Move(minIndex + 1, maxIndex);
	}

	public T[] ToArray()
	{
		try
		{
			EnterReadLock();
			var snapshot = new T[_activeItems.Count];
			_activeItems.CopyTo(snapshot, 0);
			return snapshot;
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <summary>
	/// Try to get an item then remove it.
	/// </summary>
	/// <param name="index"> The index to get the item from. </param>
	/// <param name="item"> The item retrieved or default. </param>
	/// <returns> True if the item was available and retrieved otherwise false. </returns>
	public bool TryGetAndRemoveAt(int index, out T item)
	{
		try
		{
			EnterUpgradeableReadLock();

			if (!InternalHasIndex(index))
			{
				item = default;
				return false;
			}

			item = _activeItems[index];
			InternalRemoveAt(index);
			return true;
		}
		finally
		{
			ExitUpgradeableReadLock();
		}
	}

	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		if (update is IEnumerable<T> list)
		{
			this.Reconcile(list);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Raises the <see cref="CollectionChanged" /> event.
	/// </summary>
	/// <param name="e"> A <see cref="NotifyCollectionChangedEventArgs" /> describing the event arguments. </param>
	protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	{
		CollectionChanged?.Invoke(this, e);
	}

	/// <summary>
	/// Used to invoke the <see cref="ListUpdated" /> event.
	/// </summary>
	/// <param name="added"> The items added. </param>
	/// <param name="removed"> The items removed. </param>
	protected void OnListUpdated(T[] added, T[] removed)
	{
		OnListUpdated(new SpeedyListUpdatedEventArg<T>(added, removed));
	}

	/// <summary>
	/// Used to invoke the <see cref="ListUpdated" /> event.
	/// </summary>
	/// <param name="e"> The changed event args with the details. </param>
	protected virtual void OnListUpdated(SpeedyListUpdatedEventArg<T> e)
	{
		_hasChanges = true;
		ListUpdated?.Invoke(this, e);
	}

	protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		switch (propertyName)
		{
			case nameof(FilterCheck):
			{
				RefreshFilter();
				break;
			}
			case nameof(OrderBy):
			{
				RefreshOrder();
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	internal bool InternalFilter()
	{
		if (IsFiltering
			|| _allItems.Count == 0)
		{
			return false;
		}

		IsFiltering = true;

		try
		{
			var activeHashSet = _activeItems.ToHashSet();
			var filteredSource = FilterCheck == null ? _allItems.ToHashSet() : _allItems.Where(FilterCheck).ToHashSet();
			var toRemove = activeHashSet.Where(x => !filteredSource.Contains(x)).ToArray();
			var toAdd = filteredSource.Where(x => !activeHashSet.Contains(x)).ToArray();

			if ((toRemove.Length <= 0) && (toAdd.Length <= 0))
			{
				return false;
			}

			try
			{
				EnterWriteLock();

				foreach (var item in toRemove)
				{
					var index = _activeItems.IndexOf(item);
					if (index >= 0)
					{
						_activeItems.RemoveAt(index);
					}
				}

				foreach (var item in toAdd)
				{
					_activeItems.Insert(_activeItems.Count, item);
				}

				if (ShouldOrder())
				{
					var ordered = InternalOrderCollectionForLoad(_activeItems.ToArray());
					_activeItems.Clear();
					_activeItems.AddRange(ordered);
				}
			}
			finally
			{
				ExitWriteLock();
			}

			if ((toRemove.Length > 0) || (toAdd.Length > 0))
			{
				this.Dispatch(() =>
				{
					OnListUpdated(toAdd, toRemove);
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
					OnPropertyChanged(nameof(Count));
				});
			}
		}
		finally
		{
			IsFiltering = false;
		}

		return true;
	}

	internal int InternalIndexOf(T item)
	{
		return InternalIndexOf(_activeItems, item, DistinctCheck);
	}

	internal static int InternalIndexOf(IList<T> list, Func<T, bool> predicate)
	{
		for (var i = 0; i < list.Count; i++)
		{
			if (predicate.Invoke(list[i]))
			{
				return i;
			}
		}

		return -1;
	}

	internal static int InternalIndexOf(IList<T> list, T item, IEqualityComparer<T> distinctCheck)
	{
		if (distinctCheck == null)
		{
			return list.IndexOf(item);
		}

		for (var i = 0; i < list.Count; i++)
		{
			if (distinctCheck.Equals(item, list[i]))
			{
				return i;
			}
		}

		return -1;
	}

	internal void InternalMove(int oldIndex, int newIndex)
	{
		T removedItem;

		try
		{
			EnterWriteLock();

			removedItem = _activeItems[oldIndex];

			// Be sure that if the last item was select we insert at count instead of the
			// requested new index because it will be (Count + 1) instead of Count (end).
			_activeItems.RemoveAt(oldIndex);
			_activeItems.Insert(newIndex > _activeItems.Count ? _activeItems.Count : newIndex, removedItem);
		}
		finally
		{
			ExitWriteLock();
		}

		this.Dispatch(() =>
		{
			var e = new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Move,
				removedItem,
				newIndex,
				oldIndex
			);
			OnCollectionChanged(e);
		});
	}

	void ICollection<T>.Add(T item)
	{
		AddWithLock(item);
	}

	int IList.Add(object item)
	{
		if (item is not T value)
		{
			throw new ArgumentException("The item is the incorrect value type.", nameof(item));
		}

		return AddWithLock(value);
	}

	private int AddWithLock(T item)
	{
		try
		{
			EnterUpgradeableReadLock();

			if (InternalIndexOf(_allItems, item, DistinctCheck) < 0)
			{
				_allItems.Add(item);
			}

			if ((FilterCheck == null) || FilterCheck(item))
			{
				var response = InternalAdd(item);
				if (!PauseOrdering)
				{
					InternalEnforceLimit();
				}
				return response;
			}

			return -1;
		}
		finally
		{
			ExitUpgradeableReadLock();
		}
	}

	/// <inheritdoc cref="ISpeedyList" />
	private bool Contains(object item)
	{
		return item is T value && Contains(value);
	}

	/// <inheritdoc cref="ISpeedyList" />
	bool IList.Contains(object item)
	{
		return Contains(item);
	}

	void ICollection.CopyTo(Array array, int arrayIndex)
	{
		try
		{
			EnterReadLock();
			Array.Copy(_activeItems.ToArray(), 0, array, arrayIndex, _activeItems.Count);
		}
		finally
		{
			ExitReadLock();
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <inheritdoc cref="ISpeedyList" />
	private int IndexOf(object item)
	{
		if (item is not T value)
		{
			return -1;
		}

		return IndexOf(value);
	}

	/// <inheritdoc cref="ISpeedyList" />
	int IList.IndexOf(object item)
	{
		return IndexOf(item);
	}

	private int InternalAdd(T item)
	{
		if (InternalDistinct(item, out var index))
		{
			return index;
		}

		if (ShouldOrder() && OrderBy is { Length: > 0 })
		{
			var insertIndex = this.GetInsertIndex(item, OrderBy);
			if (insertIndex >= 0)
			{
				InternalInsert(insertIndex, item);
				return insertIndex;
			}
		}

		index = _activeItems.Count;
		InternalInsert(index, item);

		if (ShouldOrder())
		{
			// This is a custom order
			InternalOrderWithoutLocking();
		}

		return index;
	}

	private bool InternalContains(T item)
	{
		var index = InternalIndexOf(item);
		return index >= 0;
	}

	private bool InternalDistinct(T item, out int index)
	{
		var function = DistinctCheck;

		if (function != null)
		{
			index = InternalIndexOf(item);
			if (index >= 0)
			{
				return true;
			}
		}

		index = -1;
		return false;
	}

	private void InternalEnforceLimit()
	{
		var limitFromStart = OrderBy is null or { Length: <= 0 };
		InternalEnforceLimit(limitFromStart);
	}

	private void InternalEnforceLimit(bool start)
	{
		while (_activeItems.Count > Limit)
		{
			InternalRemoveAt(start ? 0 : _activeItems.Count - 1);
		}
	}

	private bool InternalHasIndex(int index)
	{
		return (index >= 0) && (index < _activeItems.Count);
	}

	private bool InternalInsert(int index, T item)
	{
		// Distinct check should have already been done

		try
		{
			EnterWriteLock();

			_activeItems.Insert(index, item);
		}
		finally
		{
			ExitWriteLock();
		}

		this.Dispatch(() =>
		{
			OnListUpdated([item], null);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
			OnPropertyChanged(nameof(Count));
		});

		return true;
	}

	/// <summary>
	/// Loads the items into the list. All existing items will be cleared.
	/// </summary>
	/// <param name="items"> The items to be loaded. </param>
	private void InternalLoad(params T[] items)
	{
		IsLoading = true;

		try
		{
			Clear();

			if (items?.Length <= 0)
			{
				return;
			}

			// Guarantee uniqueness of items if we have a comparer
			var processedItems = DistinctCheck != null
				? items.ToHashSet(DistinctCheck).ToArray()
				: items;

			_allItems.AddRange(processedItems);

			processedItems = InternalOrderCollectionForLoad(processedItems);

			// See if we should limit the collection
			if (processedItems.Length > Limit)
			{
				// Limit to the set limit
				processedItems = processedItems.Take(Limit).ToArray();
			}

			if (FilterCheck != null)
			{
				processedItems = processedItems.Where(FilterCheck).ToArray();
			}

			try
			{
				EnterWriteLock();

				_activeItems.AddRange(processedItems);
			}
			finally
			{
				ExitWriteLock();
			}

			this.Dispatch(() =>
			{
				OnListUpdated(processedItems.ToArray(), null);
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
				OnPropertyChanged(nameof(Count));
			});
		}
		finally
		{
			IsLoading = false;
		}
	}

	private T[] InternalOrderCollectionForLoad(T[] items)
	{
		if ((items.Length <= 1) || OrderBy is not { Length: > 0 })
		{
			return items;
		}

		return items.Order(OrderBy).ToArray();
	}

	private void InternalOrderWithoutLocking(bool force = false)
	{
		if (!ShouldOrder() && !force)
		{
			return;
		}

		try
		{
			IsOrdering = true;
			var ordered = _activeItems.Order(OrderBy).ToList();

			for (var i = 0; i < ordered.Count; i++)
			{
				var currentItem = ordered[i];
				var index = InternalIndexOf(currentItem);

				if ((index != -1) && (index != i))
				{
					InternalMove(index, i);
				}
			}
		}
		finally
		{
			IsOrdering = false;
		}
	}

	private void InternalRemoveAt(int index)
	{
		T removedItem;

		try
		{
			EnterWriteLock();

			removedItem = _activeItems[index];
			_activeItems.RemoveAt(index);
			_allItems.Remove(removedItem);
		}
		finally
		{
			ExitWriteLock();
		}

		this.Dispatch(() =>
		{
			OnListUpdated(null, [removedItem]);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
			OnPropertyChanged(nameof(Count));
		});
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

	public event EventHandler<SpeedyListUpdatedEventArg<T>> ListUpdated;

	#endregion
}

/// <summary>
/// Represents a speedy list.
/// </summary>
[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface ISpeedyList<T> : IList<T>
{
	#region Properties

	/// <summary>
	/// An optional comparer to use if you want a distinct list.
	/// </summary>
	IEqualityComparer<T> DistinctCheck { get; set; }

	/// <summary>
	/// An optional filter to restrict the collection.
	/// </summary>
	Func<T, bool> FilterCheck { get; set; }

	/// <summary>
	/// The expression to order this collection by.
	/// </summary>
	OrderBy<T>[] OrderBy { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Removes all items from the list.
	/// </summary>
	new void Clear();

	/// <summary>
	/// Determines the index of a specific item based on the predicate.
	/// </summary>
	/// <param name="predicate"> The predicate to locate the item with. </param>
	/// <returns> The index of item if found otherwise -1 if no matches. </returns>
	int IndexOf(Func<T, bool> predicate);

	/// <summary>
	/// Loads the items into the list. All existing items will be cleared.
	/// </summary>
	/// <param name="items"> The items to be loaded. </param>
	void Load(IEnumerable items);

	/// <summary>
	/// Process an action then order the collection.
	/// </summary>
	/// <param name="process"> The action. </param>
	void ProcessThenOrder(Action process);

	/// <summary>
	/// Refresh the filter.
	/// </summary>
	void RefreshFilter();

	#endregion

	#region Events

	/// <summary>
	/// Used to notify when items are added or removed.
	/// Note: this includes all items. These items may not be available if filtered out.
	/// </summary>
	event EventHandler<SpeedyListUpdatedEventArg<T>> ListUpdated;

	#endregion
}

public interface ISpeedyList : IList, INotifyCollectionChanged, IDispatchable
{
	#region Properties

	/// <summary>
	/// True if the list is currently filtering items.
	/// </summary>
	bool IsFiltering { get; }

	/// <summary>
	/// True if the list is currently loading items.
	/// </summary>
	bool IsLoading { get; }

	/// <summary>
	/// True if the list is currently ordering items.
	/// </summary>
	bool IsOrdering { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Loads the provided list. All existing items will be cleared.
	/// </summary>
	/// <param name="list"> The list to be loaded. </param>
	void Load(IEnumerable list);

	/// <summary>
	/// Moves an item to a new index.
	/// </summary>
	/// <param name="oldIndex"> The index of the item to move. </param>
	/// <param name="newIndex"> The index to move the item to. </param>
	void Move(int oldIndex, int newIndex);

	/// <summary>
	/// Refresh the filter.
	/// </summary>
	void RefreshFilter();

	/// <summary>
	/// Refresh the order.
	/// </summary>
	public void RefreshOrder(bool force = false);

	/// <summary>
	/// Swap items in two location.
	/// </summary>
	/// <param name="firstIndex"> The index of the item to move. </param>
	/// <param name="secondIndex"> The index of the item to move. </param>
	void Swap(int firstIndex, int secondIndex);

	/// <summary>
	/// Determine if the list should order.
	/// </summary>
	/// <returns> True if the list should order or false otherwise. </returns>
	internal bool ShouldOrder();

	#endregion
}