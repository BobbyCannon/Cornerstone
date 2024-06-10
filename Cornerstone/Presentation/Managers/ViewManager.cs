#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Cornerstone.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Presentation.Managers;

/// <summary>
/// Represents a manager of a set of views.
/// </summary>
public abstract class ViewManager<T> : Manager, IViewManager<T>
{
	#region Fields

	private readonly SpeedyList<T> _collection;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the view manager.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	/// <param name="items"> An optional set. </param>
	protected ViewManager(IDispatcher dispatcher, params T[] items)
		: this(dispatcher, [], items)
	{
	}

	/// <summary>
	/// Initialize the view manager.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	/// <param name="orderBy"> The optional set of order by settings. </param>
	/// <param name="items"> An optional set. </param>
	protected ViewManager(IDispatcher dispatcher, OrderBy<T>[] orderBy, params T[] items)
		: base(dispatcher)
	{
		_collection = new SpeedyList<T>(dispatcher, orderBy, items);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The quantity of items in the view manager.
	/// </summary>
	public int Count => _collection.Count;

	/// <inheritdoc />
	public bool IsLoaded { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Add a new item to the view manager.
	/// </summary>
	/// <param name="item"> The item to be added. </param>
	/// <returns> The added item. </returns>
	public T2 Add<T2>(T2 item) where T2 : T
	{
		_collection.Add(item);
		return item;
	}

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
	{
		return _collection.GetEnumerator();
	}

	/// <inheritdoc />
	public override void Initialize()
	{
		_collection.ListUpdated += OnCollectionUpdated;
		_collection.CollectionChanged += CollectionOnCollectionChanged;
		base.Initialize();
	}

	/// <inheritdoc />
	public void Load()
	{
		IsLoaded = true;
	}

	/// <inheritdoc />
	public override void Uninitialize()
	{
		_collection.ListUpdated -= OnCollectionUpdated;
		_collection.CollectionChanged -= CollectionOnCollectionChanged;
		base.Uninitialize();
	}

	/// <inheritdoc />
	public void Unload()
	{
		IsLoaded = false;
	}

	/// <summary>
	/// The collection was updated by adding or removing items.
	/// </summary>
	/// <param name="args"> The collection items that was added or removed. </param>
	protected virtual void OnCollectionUpdated(SpeedyListUpdatedEventArg<T> args)
	{
		args.Removed?
			.ForEach(x =>
			{
				if (x is IViewModel viewModel)
				{
					viewModel.Uninitialize();
				}

				if (x is IDisposable disposable)
				{
					disposable.Dispose();
				}
			});

		args.Added?
			.ForEach(x =>
			{
				if (x is IViewModel viewModel)
				{
					viewModel.Initialize();
				}
			});

		OnPropertyChanged(nameof(Count));
	}

	private void CollectionOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		CollectionChanged?.Invoke(this, e);
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private void OnCollectionUpdated(object sender, SpeedyListUpdatedEventArg<T> e)
	{
		OnCollectionUpdated(e);
	}

	/// <inheritdoc />
	void IViewManager.Remove(object value)
	{
		_collection.Remove((T) value);
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event NotifyCollectionChangedEventHandler CollectionChanged;

	#endregion
}

/// <summary>
/// Represents a manager for a collection of views.
/// </summary>
public interface IViewManager<out T> : IViewManager, IEnumerable<T>, INotifyCollectionChanged
{
	#region Properties

	/// <summary>
	/// The manager is loaded.
	/// </summary>
	bool IsLoaded { get; }

	#endregion

	#region Methods

	/// <summary>
	/// The method to load the manager.
	/// </summary>
	void Load();

	/// <summary>
	/// The method to unload the manager.
	/// </summary>
	void Unload();

	#endregion
}

/// <summary>
/// Represents a manager for a collection of views.
/// </summary>
public interface IViewManager : IViewModel
{
	#region Methods

	/// <summary>
	/// Remove an item from the view
	/// </summary>
	/// <param name="value"> </param>
	void Remove(object value);

	#endregion
}