#region References

using System;
using System.Diagnostics;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Managers;
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
		IDispatcher dispatcher
	) : base(dispatcher)
	{
		KeyCache = databaseKeyCache;
		Settings = databaseSettings;
	}

	#endregion

	#region Properties

	public DatabaseKeyCache KeyCache { get; set; }

	public DatabaseSettings Settings { get; set; }

	#endregion

	#region Methods

	public override void Initialize()
	{
		KeyCache?.InitializeAndLoad(this, Settings.SyncOrder);
		base.Initialize();
	}

	public T GetDatabase()
	{
		return GetSyncableDatabase(Settings, KeyCache);
	}

	public T GetDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return GetSyncableDatabase(settings, keyCache);
	}

	/// <inheritdoc />
	public T GetSyncableDatabase()
	{
		return GetSyncableDatabase(Settings, KeyCache);
	}

	public T GetSyncableDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return Migrate(GetDatabaseFromManager(settings, keyCache));
	}

	protected abstract T GetDatabaseFromManager(DatabaseSettings settings, DatabaseKeyCache keyCache);

	protected virtual bool HandleFailedMigration(T database)
	{
		return false;
	}

	protected T Migrate(T database)
	{
		return Migrate(database, true);
	}

	protected virtual T Migrate(T database, bool canRetry)
	{
		if (!ShouldMigrate())
		{
			return database;
		}

		var retryFailedMigration = false;

		try
		{
			database.Migrate();

			// Store the migration version, so we can know it was migrated
			WriteMigrationVersion();
		}
		catch (InvalidOperationException)
		{
			Debugger.Break();
			retryFailedMigration = HandleFailedMigration(database);
		}
		catch (Exception)
		{
			retryFailedMigration = HandleFailedMigration(database);
		}

		if (retryFailedMigration && canRetry)
		{
			Migrate(database, false);
		}

		return database;
	}

	protected virtual bool ShouldMigrate()
	{
		return true;
	}

	protected virtual void WriteMigrationVersion()
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