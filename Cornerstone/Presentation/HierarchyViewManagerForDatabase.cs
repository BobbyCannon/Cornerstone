#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Presentation;

public abstract class HierarchyViewManagerForDatabase<TModel, TEntity, TEntityKey, TDatabase>
	: HierarchyViewManager<TModel>
	where TModel : class, ISpeedyTree<TModel>, IHierarchyItem, IHierarchySyncItem, IUpdateable
	where TEntity : SyncEntity<TEntityKey>, IClientEntity, IHierarchySyncItem
	where TDatabase : ISyncableDatabase
{
	#region Constructors

	protected HierarchyViewManagerForDatabase(
		IDatabaseProvider<TDatabase> databaseProvider,
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		Func<TModel, TModel, bool> distinctCheck,
		params OrderBy<TModel>[] orderBy
	) : base(dateTimeProvider, dependencyProvider, dispatcher, distinctCheck, orderBy)
	{
		DatabaseProvider = databaseProvider;
	}

	#endregion

	#region Properties

	public IDatabaseProvider<TDatabase> DatabaseProvider { get; }

	/// <summary>
	/// Data to include when Loading or Refreshing the manager.
	/// </summary>
	protected virtual Expression<Func<TEntity, object>>[] Included => [];

	protected virtual Func<TEntity, bool> LoadPredicate => x => !x.IsDeleted;

	protected virtual Func<TModel, TEntity, bool> LookupPredicate => (m, e) => m.SyncId == e.SyncId;

	protected virtual Func<TEntity, bool> RefreshPredicate =>
		LastUpdated == DateTime.MinValue
			? x => x.LastClientUpdate >= LastUpdated
			: x => x.LastClientUpdate > LastUpdated;

	protected virtual Func<TEntity, bool> RemovePredicateByEntity => x => x.IsDeleted;

	protected virtual Func<TEntity, bool> UpdatePredicate => x => !x.IsDeleted;

	#endregion

	#region Methods

	/// <summary>
	/// Add or update the view by using the entity.
	/// </summary>
	/// <param name="update"> The entity update. </param>
	/// <returns> The view that was added or updated. </returns>
	public TModel AddOrUpdate(TEntity update)
	{
		// Locate account view to update, or see if our account is a view,
		// or build a new account view from the account
		var foundView = FirstOrDefaultDescendants(x => LookupPredicate.Invoke(x, update));

		if (foundView == null)
		{
			foundView = CreateView();
			UpdateView(foundView, update);
			var parent = LocateParent(update);
			parent.Add(foundView);
			OnViewUpdated(foundView);
			return foundView;
		}

		if (UpdateView(foundView, update))
		{
			OnViewUpdated(foundView);
		}
		return foundView;
	}

	public virtual IEnumerable<TModel> AddOrUpdate(params TEntity[] updates)
	{
		// Remove entities that should be removed
		updates
			.Where(RemovePredicateByEntity)
			.ForEach(x => Tree.Remove(v => LookupPredicate(v, x)));

		// Add or update new items
		var updatedViews = updates
			.Where(UpdatePredicate)
			.Select(AddOrUpdate)
			.ToList();

		return updatedViews;
	}

	public override void InitializeLifecycle()
	{
		if (!IsLifecycleInitialized())
		{
			LoadFromDatabase();
		}
		base.InitializeLifecycle();
	}

	/// <summary>
	/// Called to loads the views from the database.
	/// This should be call only once and the first call.
	/// </summary>
	/// <returns> True if there were changes otherwise false. </returns>
	public virtual bool LoadFromDatabase()
	{
		// This is called after sync which could be on another thread other than dispatcher.
		if (!TryGetEntitiesToLoad(out var updatedEntities, out var until))
		{
			return false;
		}

		if (updatedEntities.Length <= 0)
		{
			LastUpdated = until;
			ResetHasChanges();
			return false;
		}

		var orderedEntities = HierarchyExtensions.Order(updatedEntities);

		AddOrUpdate(orderedEntities);
		LastUpdated = until;
		ResetHasChanges();

		return true;
	}

	/// <summary>
	/// Called to refresh the view from the database.
	/// From the time last refresh or loaded until now.
	/// </summary>
	/// <returns> True if there were changes otherwise false. </returns>
	public virtual bool RefreshFromDatabase()
	{
		// This is called after sync which could be on another thread other than dispatcher.
		if (!TryGetEntitiesToRefresh(out var updatedEntities, out var until))
		{
			return false;
		}

		if (updatedEntities.Length <= 0)
		{
			LastUpdated = until;
			return false;
		}

		var orderedEntities = HierarchyExtensions.Order(updatedEntities);

		AddOrUpdate(orderedEntities);
		LastUpdated = until;

		return true;
	}

	protected virtual bool TryGetEntitiesToLoad(out TEntity[] entities, out DateTime until)
	{
		CheckIfManagerShouldRefresh(out var now);

		using var database = DatabaseProvider.GetDatabase();
		var repo = database.GetReadOnlyRepository<TEntity, TEntityKey>();

		if (Included is { Length: > 0 })
		{
			entities = repo
				.Including(Included)
				.Where(LoadPredicate)
				.Where(x => x.LastClientUpdate <= now)
				.ToArray();
		}
		else
		{
			entities = repo
				.Where(LoadPredicate)
				.Where(x => x.LastClientUpdate <= now)
				.ToArray();
		}

		until = now;
		return true;
	}

	protected virtual bool TryGetEntitiesToRefresh(out TEntity[] entities, out DateTime until)
	{
		if (!CheckIfManagerShouldRefresh(out var now))
		{
			entities = [];
			until = DateTime.MinValue;
			return false;
		}

		using var database = DatabaseProvider.GetDatabase();
		var repo = database.GetReadOnlyRepository<TEntity, TEntityKey>();

		if (Included is { Length: > 0 })
		{
			entities = repo
				.Including(Included)
				.Where(RefreshPredicate)
				.Where(x => x.LastClientUpdate <= now)
				.ToArray();
		}
		else
		{
			entities = repo
				.Where(RefreshPredicate)
				.Where(x => x.LastClientUpdate <= now)
				.ToArray();
		}

		until = now;
		return true;
	}

	#endregion
}