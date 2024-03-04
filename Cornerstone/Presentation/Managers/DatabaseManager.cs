#region References

using System;
using System.IO;
using Cornerstone.Extensions;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cornerstone.Presentation.Managers;

/// <summary>
/// Handles management of a syncable database for application client database.
/// </summary>
/// <typeparam name="T"> The type that represents the database </typeparam>
public abstract class ClientDatabaseManager<T> : Manager, ISyncableDatabaseProvider<T>
	where T : ISyncableDatabase
{
	#region Fields

	private string _databaseFilePath, _migrationFilePath;
	private bool _isMigrated;

	#endregion

	#region Constructors

	protected ClientDatabaseManager(string databaseName, string storagePath, string version, IDispatcher dispatcher) : base(dispatcher)
	{
		DatabaseName = databaseName;
		StoragePath = storagePath;
		Version = version ?? string.Empty;
	}

	#endregion

	#region Properties

	public string DatabaseFilePath => _databaseFilePath ??= Path.Combine(StoragePath, $"{DatabaseName}.db");

	public string DatabaseName { get; }

	public DatabaseKeyCache KeyCache { get; set; }

	public string MigrationFilePath => _migrationFilePath ??= Path.Combine(StoragePath, $"{DatabaseName}.db.version");

	public DatabaseOptions Options { get; set; }

	public string StoragePath { get; }

	public string Version { get; set; }

	#endregion

	#region Methods

	public string GetConnectionString()
	{
		ValidateDatabaseName();
		return $"Data Source={DatabaseFilePath}";
	}

	public T GetDatabase()
	{
		var options = new DatabaseOptions();
		options.UpdateWith(Options);
		return GetDatabase(options);
	}

	public T GetDatabase(DatabaseOptions options)
	{
		return GetSyncableDatabase(options, KeyCache);
	}

	public T GetSyncableDatabase(DatabaseOptions options, DatabaseKeyCache keyCache)
	{
		var database = GetDatabaseFromManager(options, keyCache);

		if (!_isMigrated)
		{
			database = Migrate(database, () => { });
			_isMigrated = true;
		}

		return database;
	}

	public T GetSyncableDatabase()
	{
		var options = new DatabaseOptions();
		options.UpdateWith(Options);
		return GetSyncableDatabase(options, KeyCache);
	}

	public override void Initialize()
	{
		// Refresh the migration status
		new DirectoryInfo(StoragePath).SafeCreate();
		LoadMigrationStatus();
		base.Initialize();
	}

	public void LoadMigrationStatus()
	{
		try
		{
			_isMigrated = Version == ReadMigrationVersion();
		}
		catch
		{
			_isMigrated = false;
		}
	}

	/// <summary>
	/// Reads the version from the migration file.
	/// </summary>
	/// <returns> The last version migrated. </returns>
	public virtual string ReadMigrationVersion()
	{
		try
		{
			return File.Exists(MigrationFilePath) ? File.ReadAllText(MigrationFilePath) : string.Empty;
		}
		catch
		{
			return string.Empty;
		}
	}

	/// <summary>
	/// Reset the database, clears the data.
	/// </summary>
	public override void Uninitialize()
	{
		_isMigrated = false;
		_databaseFilePath = null;
		_migrationFilePath = null;

		KeyCache.Clear();
	}

	public IDatabaseSession<T> StartSession()
	{
		return new DatabaseSession<T>(this);
	}

	/// <summary>
	/// Writes the current version to the migration file to track migrations.
	/// </summary>
	public virtual void WriteMigrationVersion()
	{
		ValidateDatabaseName();

		if (!string.IsNullOrWhiteSpace(Version))
		{
			File.WriteAllText(MigrationFilePath, Version);
		}
	}

	protected abstract T GetDatabaseFromManager(DatabaseOptions options, DatabaseKeyCache keyCache);

	protected virtual T Migrate(T database, Action failed)
	{
		if (!database.CanMigrate())
		{
			return database;
		}

		try
		{
			database.Migrate();

			// Store the migration version, so we can know it was migrated
			WriteMigrationVersion();
		}
		catch (Exception)
		{
			// todo: remove database?

			// Reset the database
			database.Dispose();
			database = GetDatabaseFromManager(database.Options, database.KeyCache);
			database.Migrate();

			// Store the migration version, so we can know it was migrated
			WriteMigrationVersion();

			failed();
		}

		return database;
	}

	protected void ValidateDatabaseName()
	{
		if (DatabaseName == null)
		{
			throw new InvalidOperationException("Database name is not set and should be!");
		}
	}

	IDatabase IDatabaseProvider.GetDatabase(DatabaseOptions options)
	{
		return GetDatabase(options);
	}

	IDatabase IDatabaseProvider.GetDatabase()
	{
		return GetDatabase();
	}

	ISyncableDatabase ISyncableDatabaseProvider.GetSyncableDatabase()
	{
		return GetSyncableDatabase();
	}

	#endregion
}