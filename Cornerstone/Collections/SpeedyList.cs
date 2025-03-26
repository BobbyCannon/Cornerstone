#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Threading;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// A thread-safe, dispatch safe, limitable, orderable, filterable, and observable list.
/// Dispatch safe, limit, orderable, and filterable settings are optional.
/// </summary>
/// <typeparam name="T"> The type of items in the list. </typeparam>
public class SpeedyList<T> : ReaderWriterLockBindable, ISpeedyList<T>, IList
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
	public Func<T, T, bool> DistinctCheck { get; set; }

	/// <summary>
	/// An optional filter to restrict the collection.
	/// </summary>
	public Func<T, bool> FilterCheck { get; set; }

	/// <summary>
	/// True if the list is currently filtering items.
	/// </summary>
	public bool IsFiltering { get; private set; }

	/// <inheritdoc cref="ISpeedyList" />
	public bool IsFixedSize => false;

	/// <summary>
	/// True if the list is currently loading items.
	/// </summary>
	public bool IsLoading { get; private set; }

	/// <summary>
	/// True if the list is in the process of ordering.
	/// </summary>
	public bool IsOrdering { get; protected set; }

	/// <inheritdoc cref="ISpeedyList" />
	public bool IsReadOnly => false;

	/// <inheritdoc />
	public bool IsSynchronized => true;

	/// <inheritdoc />
	[SuppressPropertyChangedWarnings]
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
	public int Limit { get; set; }

	/// <summary>
	/// The expression to order this collection by.
	/// </summary>
	public OrderBy<T>[] OrderBy { get; set; }

	/// <summary>
	/// Flag to track pausing of ordering.
	/// </summary>
	public bool PauseOrdering { get; private set; }

	/// <summary>
	/// The profiler for the list. <see cref="InitializeProfiler" /> must be called before accessing the profiler.
	/// </summary>
	public SpeedyListProfiler Profiler { get; private set; }

	/// <inheritdoc cref="ISpeedyList" />
	public object SyncRoot { get; }

	/// <inheritdoc />
	[SuppressPropertyChangedWarnings]
	object ISpeedyList.this[int index]
	{
		get => this[index];
		set => this[index] = (T) value;
	}

	/// <inheritdoc />
	[SuppressPropertyChangedWarnings]
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

			Profiler?.RemovedCount.Increment(removedItems.Length);
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
	{
		try
		{
			EnterReadLock();
			var list = _activeItems.ToList();
			return list.GetEnumerator();
		}
		finally
		{
			ExitReadLock();
		}
	}

	/// <inheritdoc />
	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return _hasChanges;
	}

	/// <inheritdoc />
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

	/// <summary>
	/// Initialize the profiler to allow tracking of list events.
	/// </summary>
	public void InitializeProfiler()
	{
		Profiler ??= new SpeedyListProfiler(GetDispatcher());
	}

	/// <inheritdoc />
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
				if (InternalDistinct(item, out _))
				{
					return;
				}

				if (!InternalInsert(index, item))
				{
					return;
				}
			}

			InternalOrderWithoutLocking();
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
		}
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	public void RefreshOrder()
	{
		if (!ShouldOrder())
		{
			return;
		}

		try
		{
			EnterUpgradeableReadLock();
			InternalOrderWithoutLocking();
		}
		finally
		{
			ExitUpgradeableReadLock();
		}
	}

	/// <inheritdoc />
	public bool Remove(T item)
	{
		T removedItem;
		int index;

		try
		{
			EnterWriteLock();

			index = InternalIndexOf(item);
			if (index < 0)
			{
				return false;
			}

			removedItem = _activeItems[index];

			_activeItems.RemoveAt(index);
			_allItems.Remove(item);

			Profiler?.RemovedCount.Increment();
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
			OnListUpdated(null, [removedItem]);
			OnCollectionChanged(e);
			OnPropertyChanged(nameof(Count));
		});

		return true;
	}

	/// <summary>
	/// Remove all entries that match predicate
	/// </summary>
	/// <param name="predicate"> The predicate to find entries to remove. </param>
	public void Remove(Predicate<T> predicate)
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

				Profiler?.RemovedCount.Increment(length);
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

	/// <inheritdoc />
	public override void ResetHasChanges()
	{
		_hasChanges = false;
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

	/// <inheritdoc />
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
	[SuppressPropertyChangedWarnings]
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

	/// <inheritdoc />
	protected override void OnPropertyChangedInDispatcher(string propertyName)
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

		base.OnPropertyChangedInDispatcher(propertyName);
	}

	internal bool InternalFilter()
	{
		if (IsFiltering)
		{
			return false;
		}

		IsFiltering = true;

		try
		{
			var currentItems = _activeItems.ToArray();
			var filteredSource = FilterCheck == null ? _allItems.ToArray() : _allItems.Where(FilterCheck).ToArray();
			var toRemove = currentItems.Where(x => !filteredSource.Contains(x)).ToArray();
			var toAdd = filteredSource
				.Where(x => !_activeItems.Contains(x))
				.ToArray();

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
					_activeItems.Add(ordered);
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

	internal static int InternalIndexOf(IList<T> list, T item, Func<T, T, bool> distinctCheck)
	{
		if (distinctCheck == null)
		{
			return list.IndexOf(item);
		}

		for (var i = 0; i < list.Count; i++)
		{
			if (distinctCheck.Invoke(item, list[i]))
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

	/// <inheritdoc />
	void ISpeedyList.Add(object item)
	{
		if (item is not T value)
		{
			throw new ArgumentException("The item is the incorrect value type.", nameof(item));
		}

		AddWithLock(value);
	}

	/// <inheritdoc />
	void ICollection<T>.Add(T item)
	{
		AddWithLock(item);
	}

	/// <inheritdoc />
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
				var limitFromStart = !ShouldOrder();
				InternalEnforceLimit(limitFromStart);
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

	/// <inheritdoc cref="ISpeedyList" />
	bool ISpeedyList.Contains(object item)
	{
		return Contains(item);
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc cref="ISpeedyList" />
	int ISpeedyList.IndexOf(object item)
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
			var insertIndex = OrderBy<T>.GetInsertIndex(this, item, OrderBy);
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

			Profiler?.AddedCount.Increment();
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
				? items.Distinct(new EqualityComparer<T>(DistinctCheck)).ToArray()
				: items.ToArray();

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

				Profiler?.AddedCount.Increment(processedItems.Length);
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

		var firstOrder = OrderBy.First();
		var thenBy = OrderBy.Skip(1).ToArray();
		var ordered = firstOrder.Process(items, thenBy).ToArray();
		return ordered;
	}

	private void InternalOrderWithoutLocking()
	{
		if (!ShouldOrder())
		{
			return;
		}

		try
		{
			IsOrdering = true;
			Profiler?.OrderCount.Increment();

			var firstOrder = OrderBy.First();
			var thenBy = OrderBy.Skip(1).ToArray();
			var ordered = firstOrder.Process(_activeItems.AsQueryable(), thenBy).ToList();

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

			Profiler?.RemovedCount.Increment();
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

	/// <inheritdoc />
	public event EventHandler<SpeedyListUpdatedEventArg<T>> ListUpdated;

	#endregion
}

/// <summary>
/// Represents a speedy list.
/// </summary>
[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface ISpeedyList<T> : IList<T>, ISpeedyList
{
	#region Properties

	/// <summary>
	/// An optional filter to restrict the collection.
	/// </summary>
	Func<T, bool> FilterCheck { get; set; }

	/// <summary>
	/// The expression to order this collection by.
	/// </summary>
	public OrderBy<T>[] OrderBy { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Loads the items into the list. All existing items will be cleared.
	/// </summary>
	/// <param name="items"> The items to be loaded. </param>
	void Load(params T[] items);

	#endregion

	#region Events

	/// <summary>
	/// Used to notify when items are added or removed.
	/// Note: this includes all items. These items may not be available if filtered out.
	/// </summary>
	event EventHandler<SpeedyListUpdatedEventArg<T>> ListUpdated;

	#endregion
}

public interface ISpeedyList : IEnumerable, INotifyCollectionChanged, IDispatchable
{
	#region Properties

	/// <summary>
	/// Gets the number of elements contained in the list.
	/// </summary>
	/// <returns> The number of elements contained in the list. </returns>
	int Count { get; }

	/// <summary>
	/// True if the list is currently filtering items.
	/// </summary>
	bool IsFiltering { get; }

	/// <summary> Gets an item indicating whether the list has a fixed size. </summary>
	/// <returns>
	/// True if the list has a fixed size otherwise false.
	/// </returns>
	bool IsFixedSize { get; }

	/// <summary>
	/// True if the list is currently loading items.
	/// </summary>
	bool IsLoading { get; }

	/// <summary>
	/// True if the list is currently ordering items.
	/// </summary>
	bool IsOrdering { get; }

	/// <summary> Gets an item indicating whether the list is read-only. </summary>
	/// <returns>
	/// True if the list is read-only otherwise false.
	/// </returns>
	bool IsReadOnly { get; }

	/// <summary> Gets or sets the element at the specified index. </summary>
	/// <param name="index"> The zero-based index of the element to get or set. </param>
	/// <returns> The element at the specified index. </returns>
	object this[int index] { get; set; }

	/// <summary>
	/// Gets an object that can be used to synchronize access to the list.
	/// </summary>
	/// <returns> An object that can be used to synchronize access to the list. </returns>
	object SyncRoot { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Adds an item to the list.
	/// </summary>
	/// <param name="item"> The object to add to the list. </param>
	void Add(object item);

	/// <summary>
	/// Removes all items from the list.
	/// </summary>
	void Clear();

	/// <summary> Determines whether the list contains a specific item. </summary>
	/// <param name="item"> The object to locate in the list. </param>
	/// <returns>
	/// True if the item is found in the list otherwise false.
	/// </returns>
	bool Contains(object item);

	/// <summary>
	/// Determines the index of a specific item in the list.
	/// </summary>
	/// <param name="item"> The object to locate in the list. </param>
	/// <returns> The index of item if found in the list otherwise -1; </returns>
	int IndexOf(object item);

	/// <summary>
	/// Inserts an item to the list at the specified index.
	/// </summary>
	/// <param name="index"> The zero-based index at which item should be inserted. </param>
	/// <param name="item"> The object to insert into the list. </param>
	void Insert(int index, object item);

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
	public void RefreshOrder();

	/// <summary>
	/// Removes the first occurrence of a specific object from the list.
	/// </summary>
	/// <param name="item"> The object to remove from the list. </param>
	void Remove(object item);

	/// <summary> Removes the list item at the specified index. </summary>
	/// <param name="index"> The zero-based index of the item to remove. </param>
	void RemoveAt(int index);

	/// <summary>
	/// Determine if the list should order.
	/// </summary>
	/// <returns> True if the list should order or false otherwise. </returns>
	internal bool ShouldOrder();

	#endregion
}