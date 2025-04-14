#region References

using System;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Presentation.Managers;

public abstract class ViewManagerForDatabase<TModel, TEntity, TEntityKey, TDatabase>
	: ViewManager<TModel, TEntity, TEntityKey>
	where TModel : class, IUpdateable
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
		params OrderBy<TModel>[] orderBy)
		: base(dateTimeProvider, dependencyProvider, dispatcher, distinctCheck, orderBy)
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

	public override void Initialize()
	{
		LoadFromDatabase();
		base.Initialize();
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
			return false;
		}

		this.Dispatch(() =>
		{
			base.AddOrUpdate(updatedEntities);
			LastUpdated = until;
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

		this.Dispatch(() =>
		{
			base.AddOrUpdate(updatedEntities);
			LastUpdated = until;
		});

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