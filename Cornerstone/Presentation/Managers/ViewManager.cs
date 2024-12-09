﻿#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Presentation.Managers;

/// <summary>
/// Represents a manager of a set of views.
/// </summary>
public abstract class ViewManager<T, TEntity, TEntityKey> : ViewManager<T>
	where T : class, IUpdateable, ISyncEntity
	where TEntity : SyncEntity<TEntityKey>
{
	#region Constructors

	/// <inheritdoc />
	protected ViewManager(
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		Func<T, T, bool> distinctCheck,
		params OrderBy<T>[] orderBy)
		: base(dateTimeProvider, dependencyProvider, dispatcher, distinctCheck, orderBy)
	{
	}

	#endregion

	#region Properties

	protected virtual Func<TEntity, bool> RemovePredicateByEntity => x => x.IsDeleted;

	protected virtual Func<TEntity, bool> UpdatePredicate => x => !x.IsDeleted;

	#endregion

	#region Methods

	/// <summary>
	/// Add or update the view by using the entity.
	/// </summary>
	/// <param name="update"> The entity update. </param>
	/// <returns> The view that was added or updated. </returns>
	public T AddOrUpdate(TEntity update)
	{
		// Locate account view to update, or see if our account is a view, or build a new account view from the account
		var foundView = FirstOrDefault(x => x.SyncId == update.SyncId);

		if (foundView == null)
		{
			foundView = CreateView();
			UpdateAndInitializeView(foundView, update);
			OnViewUpdated(foundView);
			return foundView;
		}

		if (UpdateView(foundView, update))
		{
			OnViewUpdated(foundView);
		}
		return foundView;
	}

	public virtual IEnumerable<T> AddOrUpdate(params TEntity[] updates)
	{
		return ProcessThenOrder(() =>
		{
			// Remove view that should be removed
			RemoveViews();

			// Remove entities that should be removed
			updates
				.Where(RemovePredicateByEntity)
				.ForEach(x => RemoveBySyncId(x.SyncId));

			// Add or update new items
			var updatedViews = updates
				.Where(UpdatePredicate)
				.Select(AddOrUpdate)
				.ToList();

			return updatedViews;
		});
	}

	public T RemoveBySyncId(Guid id)
	{
		// Need to ensure this is thread safe
		var view = FirstOrDefault(x => x.SyncId == id);
		if (view != null)
		{
			Remove(view);
		}

		return view;
	}

	#endregion
}

/// <summary>
/// Represents a manager of a set of views.
/// </summary>
public abstract class ViewManager<T>
	: SpeedyList<T>, IDisposable, IManager
	where T : class, IUpdateable
{
	#region Constructors

	protected ViewManager(
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		Func<T, T, bool> distinctCheck,
		params OrderBy<T>[] orderBy)
		: base(dispatcher, orderBy)
	{
		DateTimeProvider = dateTimeProvider;
		DependencyProvider = dependencyProvider;
		DistinctCheck = distinctCheck;
	}

	#endregion

	#region Properties

	public IDateTimeProvider DateTimeProvider { get; }

	public IDependencyProvider DependencyProvider { get; }

	/// <inheritdoc />
	public bool IsInitialized { get; private set; }

	/// <summary>
	/// The last time this view was updated.
	/// </summary>
	public DateTime LastUpdated { get; set; }

	/// <summary>
	/// Gets the selected view.
	/// </summary>
	public T SelectedView { get; set; }

	/// <summary>
	/// Predicate for removing views from collection
	/// </summary>
	protected virtual Func<T, bool> RemovePredicateByView => x => false;

	#endregion

	#region Methods

	/// <summary>
	/// NOTE: Be careful when using this because it does not perform as well as "AddOrUpdateViews"
	/// </summary>
	/// <param name="update"> The update. </param>
	/// <returns> </returns>
	public T AddOrUpdate(T update)
	{
		var foundView = FirstOrDefault(x => DistinctCheck(x, update));
		if (foundView == null)
		{
			foundView = update;
			UpdateAndInitializeView(foundView, update);
			Add(update);
			OnViewUpdated(update);
			return update;
		}

		if (UpdateView(foundView, update))
		{
			OnViewUpdated(foundView);
		}
		return foundView;
	}

	/// <summary>
	/// Add or update the view by using an object very similar to the view.
	/// It is very critical that this is used sparingly as the object could get
	/// out of sync (structure/members) with each other, then silently just
	/// not work quite right.
	/// </summary>
	/// <param name="lookup"> The lookup to locate the view. </param>
	/// <param name="update"> The update that will use . </param>
	/// <returns> The view that was added or updated. </returns>
	public T AddOrUpdate(Func<T, bool> lookup, object update)
	{
		var foundView = FirstOrDefault(lookup);
		if (foundView == null)
		{
			foundView = CreateView();
			UpdateAndInitializeView(foundView, update);
			Add(foundView);
			OnViewUpdated(foundView);
			return foundView;
		}

		if (UpdateView(foundView, update))
		{
			OnViewUpdated(foundView);
		}
		return foundView;
	}

	public override void Clear()
	{
		base.Clear();
		LastUpdated = DateTime.MinValue;
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	public virtual void Initialize()
	{
		IsInitialized = true;
	}

	/// <inheritdoc />
	public void Process()
	{
	}

	public virtual void Reset()
	{
		Clear();

		this.Dispatch(() =>
		{
			SelectedView = null;
			LastUpdated = DateTime.MinValue;
		});
	}

	/// <inheritdoc />
	public virtual void Uninitialize()
	{
		IsInitialized = false;
	}

	protected bool CheckIfManagerShouldRefresh(out DateTime until)
	{
		until = DateTimeProvider.UtcNow;
		return until > LastUpdated;
	}

	protected bool Contains(Func<T, bool> filter)
	{
		var foundView = FirstOrDefault(filter);
		return foundView != null;
	}

	protected virtual T CreateView()
	{
		return DependencyProvider.GetInstance<T>();
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		Clear();
	}

	protected override void OnListUpdated(SpeedyListUpdatedEventArg<T> e)
	{
		ProcessOldItems(e.Removed);
		ProcessNewItems(e.Added);
		base.OnListUpdated(e);
	}

	protected virtual void OnViewUpdated(T view)
	{
		ViewUpdated?.Invoke(this, view);
	}

	protected virtual void ProcessNewItems(IList<T> newItems)
	{
	}

	protected virtual void ProcessOldItems(IList<T> oldItems)
	{
	}

	protected void RemoveViews()
	{
		var itemsToRemove = this.Where(RemovePredicateByView).ToList();
		if (itemsToRemove.Count <= 0)
		{
			return;
		}

		itemsToRemove.ForEach(x => Remove(x));
	}

	protected virtual bool UpdateAndInitializeView(T view, object update)
	{
		// Just call update by default.
		return UpdateView(view, update);
	}

	protected virtual bool UpdateView(T view, object update)
	{
		if ((view == null) || (update == null))
		{
			return false;
		}

		view.UpdateWith(update);
		return true;
	}

	#endregion

	#region Events

	public event EventHandler<T> ViewUpdated;

	#endregion
}