#region References

using System;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Presentation;

public abstract class ViewManagerForDatabase<TModel, TEntity, TEntityKey, TDatabase>
	: ViewManager<TModel, TEntity, TEntityKey>, IViewManagerForDatabase
	where TModel : class, IUpdateable, new()
	where TEntity : SyncEntity<TEntityKey>, IClientEntity
	where TDatabase : ISyncableDatabase
{
	#region Constructors

	protected ViewManagerForDatabase(
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

	protected IDatabaseProvider<TDatabase> DatabaseProvider { get; }

	/// <summary>
	/// Data to include when Loading or Refreshing the manager.
	/// </summary>
	protected virtual Expression<Func<TEntity, object>>[] Included => [];

	protected virtual Func<TEntity, bool> LoadPredicate => x => !x.IsDeleted;

	protected virtual Func<TEntity, bool> RefreshPredicate =>
		LastUpdated == DateTime.MinValue
			? x => x.LastClientUpdate >= LastUpdated
			: x => x.LastClientUpdate > LastUpdated;

	#endregion

	#region Methods

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

		this.DispatchPost(() =>
		{
			AddOrUpdate(updatedEntities);
			LastUpdated = until;
			ResetHasChanges();
		});

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

		this.DispatchPost(() =>
		{
			AddOrUpdate(updatedEntities);
			LastUpdated = until;
		});

		return true;
	}

	protected override void OnListUpdated(PresentationListUpdatedEventArg<TModel> e)
	{
		foreach (var r in e.Removed)
		{
			if ((ItemBeingEdited != null)
				&& (DistinctCheck?.Equals(r, ItemBeingEdited) == true))
			{
				CancelEditItem();
			}
		}

		base.OnListUpdated(e);
	}

	protected override void OnViewUpdated(TModel view)
	{
		if ((ItemBeingEdited != null)
			&& (DistinctCheck?.Equals(view, ItemBeingEdited) == true))
		{
			CancelEditItem();
		}

		base.OnViewUpdated(view);
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

public interface IViewManagerForDatabase : IInitializableLifecycle
{
	#region Methods

	/// <summary>
	/// Called to loads the views from the database.
	/// This should be call only once and the first call.
	/// </summary>
	/// <returns> True if there were changes otherwise false. </returns>
	bool LoadFromDatabase();

	/// <summary>
	/// Called to refresh the view from the database.
	/// From the time last refresh or loaded until now.
	/// </summary>
	/// <returns> True if there were changes otherwise false. </returns>
	bool RefreshFromDatabase();

	#endregion
}