#region References

using System.Linq;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Handles management of a syncable database for application client database.
/// </summary>
/// <typeparam name="T"> The type that represents the database </typeparam>
public abstract class DatabaseManager<T>
	: Manager, ISyncableDatabaseProvider<T>
	where T : ISyncableDatabase
{
	#region Constructors

	protected DatabaseManager(
		DatabaseKeyCache databaseKeyCache,
		DatabaseSettings databaseSettings,
		Profiler profiler)
	{
		KeyCache = databaseKeyCache;
		Settings = databaseSettings;
		Profiler = profiler;
	}

	#endregion

	#region Properties

	public string ConnectionString { get; set; }

	public DatabaseKeyCache KeyCache { get; set; }

	public Profiler Profiler { get; }

	public DatabaseSettings Settings { get; set; }

	#endregion

	#region Methods

	public T GetDatabase()
	{
		return GetSyncableDatabase(Settings, KeyCache);
	}

	public T GetDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return GetSyncableDatabase(settings, keyCache);
	}

	public T GetSyncableDatabase()
	{
		return GetSyncableDatabase(Settings, KeyCache);
	}

	public T GetSyncableDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		var database = GetDatabaseFromManager(settings, keyCache);
		var isMigrated = database.IsDatabaseMigrated();
		if (!isMigrated)
		{
			Profiler.Time(nameof(database.Migrate), () => database.Migrate());
		}

		return database;
	}

	public override void LoadLifecycle()
	{
		if (KeyCache != null)
		{
			Profiler.Time("KeyCache", () => KeyCache?.InitializeAndLoad(this, Settings.SyncOrder.Select(x => x.entity).ToArray()));
		}
		base.LoadLifecycle();
	}

	protected abstract T GetDatabaseFromManager(DatabaseSettings settings, DatabaseKeyCache keyCache);

	IDatabase IDatabaseProvider.GetDatabase()
	{
		return GetDatabase();
	}

	IDatabase IDatabaseProvider.GetDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return GetDatabase(settings, keyCache);
	}

	ISyncableDatabase ISyncableDatabaseProvider.GetSyncableDatabase()
	{
		return GetSyncableDatabase();
	}

	ISyncableDatabase ISyncableDatabaseProvider.GetSyncableDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return GetSyncableDatabase(settings, keyCache);
	}

	#endregion
}