#region References

using System.Linq;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
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
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;

	#endregion

	#region Constructors

	protected DatabaseManager(
		IDateTimeProvider dateTimeProvider,
		DatabaseKeyCache databaseKeyCache,
		DatabaseSettings databaseSettings,
		Profiler profiler)
	{
		_dateTimeProvider = dateTimeProvider ?? DateTimeProvider.RealTime;
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
		var database = GetDatabaseFromManager(settings, keyCache, _dateTimeProvider);
		var isMigrated = database.IsDatabaseMigrated();
		if (!isMigrated)
		{
			Profiler.Time(nameof(database.Migrate), () =>
			{
				try
				{
					database.Migrate();
				}
				catch
				{
					OnMigrationFailed(database);
				}
			});
		}

		return database;
	}

	public override void LoadLifecycle()
	{
		if (KeyCache != null)
		{
			void LoadKeyCache()
			{
				Profiler.Time("KeyCache", () => KeyCache.InitializeAndLoad(this, Settings.SyncOrder.Select(x => x.entity).ToArray()));
			}

			var profiler = AppBootstrap.StartupProfiler;
			if (profiler != null)
			{
				profiler.Time("KeyCache", LoadKeyCache);
			}
			else
			{
				LoadKeyCache();
			}
		}
		base.LoadLifecycle();
	}

	protected abstract T GetDatabaseFromManager(
		DatabaseSettings settings,
		DatabaseKeyCache keyCache,
		IDateTimeProvider dateTimeProvider);

	protected virtual void OnMigrationFailed(T database)
	{
	}

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