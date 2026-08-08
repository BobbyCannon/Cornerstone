#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Attempt to optimize the ViewManager
/// </summary>
public class SpeedyListViewManager<T> : CornerstoneObject, IList<T>, IList, INotifyCollectionChanged, IDisposable
{
	#region Fields

	protected readonly SpeedyList<T> List;

	#endregion

	#region Constructors

	public SpeedyListViewManager(int initialCapacity = SpeedyList.DefaultCapacity, bool isLongLivedBuffer = false, bool clearOnCleanup = false)
	{
		List = new SpeedyList<T>(initialCapacity, isLongLivedBuffer, clearOnCleanup);
	}

	#endregion

	#region Properties

	public int Count => List.Count;

	public IEqualityComparer<T> DistinctCheck { get; set; }

	public bool IsFixedSize => false;

	public bool IsReadOnly => false;

	public bool IsSynchronized => false;

	public T this[int index]
	{
		get => List[index];
		set => List[index] = value;
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
		List.Add(item);
		OnCollectionChanged(NotifyCollectionChangedAction.Add, item, Count - 1);
		NotifyComputedPropertyChanged(nameof(Count));
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

	/// <summary>
	/// NOTE: Be careful when using this because it does not perform as well as "AddOrUpdateViews"
	/// </summary>
	/// <param name="update"> The update. </param>
	/// <returns> </returns>
	public T AddOrUpdate(T update)
	{
		var check = DistinctCheck ?? EqualityComparer<T>.Default;
		var foundView = FirstOrDefault(x => check.Equals(x, update));
		if (foundView == null)
		{
			foundView = update;
			UpdateView(foundView, update);
			List.Add(update);
			OnViewUpdated(update);
			return update;
		}

		if (UpdateView(foundView, update))
		{
			OnViewUpdated(foundView);
		}
		return foundView;
	}

	public ReadOnlySpan<T> AsSpan()
	{
		return List.AsSpan();
	}

	public virtual void Clear()
	{
		List.Clear();
		OnCollectionReset();
		NotifyComputedPropertyChanged(nameof(Count));
	}

	public bool Contains(T item)
	{
		return List.Contains(item);
	}

	public bool Contains(object value)
	{
		return value is T item && Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		List.CopyTo(array, arrayIndex);
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

	public virtual T FirstOrDefault(Func<T, bool> check)
	{
		return List.FirstOrDefault(check);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return ((IEnumerable<T>) List).GetEnumerator();
	}

	public int IndexOf(T item)
	{
		return List.IndexOf(item);
	}

	public int IndexOf(object value)
	{
		return value is T item ? IndexOf(item) : -1;
	}

	public void Insert(int index, T item)
	{
		List.Insert(index, item);
		OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
		NotifyComputedPropertyChanged(nameof(Count));
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

		List.RemoveAt(index);

		OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
		NotifyComputedPropertyChanged(nameof(Count));
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

	protected virtual void OnViewUpdated(T view)
	{
		ViewUpdated?.Invoke(this, view);
	}

	protected virtual bool UpdateView(T view, object update)
	{
		if ((view == null) || (update == null))
		{
			return false;
		}
		view.UpdateWith(update, IncludeExcludeSettings.Empty);
		return true;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion

	#region Events

	public event NotifyCollectionChangedEventHandler CollectionChanged;
	public event EventHandler<T> ViewUpdated;

	#endregion
}