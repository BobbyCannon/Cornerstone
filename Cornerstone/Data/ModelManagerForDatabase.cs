#region References

using System;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Collections;
using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Data;

public abstract class ModelManagerForDatabase<TModel, TEntity, TEntityKey, TDatabase>
	: ModelManager<TModel, TEntity, TEntityKey>, IModelManagerForDatabase
	where TModel : class, IUpdateable, new()
	where TEntity : SyncEntity<TEntityKey>, IClientEntity
	where TDatabase : ISyncableDatabase
{
	#region Constructors

	protected ModelManagerForDatabase(
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

	protected IDatabaseProvider<TDatabase> DatabaseProvider { get; }

	protected virtual Expression<Func<TEntity, object>>[] Included => [];

	protected virtual Func<TEntity, bool> LoadPredicate => x => !x.IsDeleted;

	protected virtual Func<TEntity, bool> RefreshPredicate =>
		LastUpdated == DateTime.MinValue
			? x => x.LastClientUpdate >= LastUpdated
			: x => x.LastClientUpdate > LastUpdated;

	#endregion

	#region Methods

	public override void LoadLifecycle()
	{
		if (!IsLifecycleLoaded())
		{
			LoadFromDatabase();
		}

		base.LoadLifecycle();
	}

	/// <summary>
	/// Load models from the database. Call once as the first fill.
	/// </summary>
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

		AddOrUpdate(updatedEntities);
		LastUpdated = until;
		ResetHasChanges();
		return true;
	}

	/// <summary>
	/// Refresh models from the database since LastUpdated.
	/// </summary>
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

		AddOrUpdate(updatedEntities);
		LastUpdated = until;
		return true;
	}

	protected override void OnListUpdated(PresentationListUpdatedEventArg<TModel> e)
	{
		if (e.Removed != null)
		{
			foreach (var r in e.Removed)
			{
				if ((ItemBeingEdited != null)
					&& (DistinctCheck?.Equals(r, ItemBeingEdited) == true))
				{
					CancelEditItem();
				}
			}
		}

		base.OnListUpdated(e);
	}

	protected override void OnModelUpdated(TModel model)
	{
		if ((ItemBeingEdited != null)
			&& (DistinctCheck?.Equals(model, ItemBeingEdited) == true))
		{
			CancelEditItem();
		}

		base.OnModelUpdated(model);
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

public interface IModelManagerForDatabase : IInitializableLifecycle
{
	#region Methods

	bool LoadFromDatabase();

	bool RefreshFromDatabase();

	#endregion
}
