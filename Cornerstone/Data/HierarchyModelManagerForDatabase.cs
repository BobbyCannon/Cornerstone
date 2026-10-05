#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Data;

public abstract class HierarchyModelManagerForDatabase<TModel, TEntity, TEntityKey, TDatabase>
	: HierarchyModelManager<TModel>
	where TModel : class, ISpeedyTree<TModel>, IHierarchyItem, IHierarchySyncItem, IUpdateable
	where TEntity : SyncEntity<TEntityKey>, IClientEntity, IHierarchySyncItem
	where TDatabase : ISyncableDatabase
{
	#region Constructors

	protected HierarchyModelManagerForDatabase(
		IDatabaseProvider<TDatabase> databaseProvider,
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		Func<TModel, TModel, bool> distinctCheck,
		params OrderBy<TModel>[] orderBy
	) : base(dateTimeProvider, dependencyProvider, distinctCheck, orderBy)
	{
		DatabaseProvider = databaseProvider;
	}

	#endregion

	#region Properties

	public IDatabaseProvider<TDatabase> DatabaseProvider { get; }

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

	public TModel AddOrUpdate(TEntity update)
	{
		var foundModel = FirstOrDefaultDescendants(x => LookupPredicate.Invoke(x, update));

		if (foundModel == null)
		{
			foundModel = CreateModel();
			UpdateModel(foundModel, update);
			var parent = LocateParent(update);
			parent.Add(foundModel);
			OnModelUpdated(foundModel);
			return foundModel;
		}

		if (UpdateModel(foundModel, update))
		{
			OnModelUpdated(foundModel);
		}
		return foundModel;
	}

	public virtual IEnumerable<TModel> AddOrUpdate(params TEntity[] updates)
	{
		updates
			.Where(RemovePredicateByEntity)
			.ForEach(x => Tree.Remove(v => LookupPredicate(v, x)));

		var updatedModels = updates
			.Where(UpdatePredicate)
			.Select(AddOrUpdate)
			.ToList();

		return updatedModels;
	}

	public override void LoadLifecycle()
	{
		if (!IsLifecycleLoaded())
		{
			LoadFromDatabase();
		}
		base.LoadLifecycle();
	}

	public virtual bool LoadFromDatabase()
	{
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

	public virtual bool RefreshFromDatabase()
	{
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
