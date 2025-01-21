#region References

using System;
using System.IO;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

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

	protected ClientDatabaseManager(IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
		: this(runtimeInformation.ApplicationName, runtimeInformation.ApplicationDataLocation, runtimeInformation.ApplicationVersion.ToString(4), dispatcher)
	{
	}

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

	public DatabaseSettings Settings { get; set; }

	public string StoragePath { get; }

	public string Version { get; set; }

	#endregion

	#region Methods

	public void EnsureMigrated()
	{
		new DirectoryInfo(StoragePath).SafeCreate();
		using var database = GetDatabase();
		Migrate(database);
	}

	/// <inheritdoc />
	public T GetDatabase()
	{
		var options = new DatabaseSettings();
		options.UpdateWith(Settings);
		return GetDatabase(options);
	}

	public T GetDatabase(DatabaseSettings settings)
	{
		return GetSyncableDatabase(settings, KeyCache);
	}

	public T GetSyncableDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		var database = GetDatabaseFromManager(settings, keyCache);

		if (!_isMigrated)
		{
			database = Migrate(database);
			_isMigrated = true;
		}

		return database;
	}

	/// <inheritdoc />
	public T GetSyncableDatabase()
	{
		var options = new DatabaseSettings();
		options.UpdateWith(Settings);
		return GetSyncableDatabase(options, KeyCache);
	}

	/// <inheritdoc />
	public abstract string[] GetSyncOrder();

	/// <inheritdoc />
	public override void Initialize()
	{
		// Refresh the migration status
		new DirectoryInfo(StoragePath).SafeCreate();
		LoadMigrationStatus();
		base.Initialize();
	}

	/// <inheritdoc />
	public override void Uninitialize()
	{
		_isMigrated = false;
		_databaseFilePath = null;
		_migrationFilePath = null;

		KeyCache?.Clear();
	}

	protected string GetConnectionString()
	{
		ValidateDatabaseName();
		return $"Data Source={DatabaseFilePath}";
	}

	protected abstract T GetDatabaseFromManager(DatabaseSettings settings, DatabaseKeyCache keyCache);

	protected virtual void HandleFailedMigration(T database)
	{
	}

	protected virtual T Migrate(T database)
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
			HandleFailedMigration(database);

			// Reset the database
			database.Dispose();
			database = GetDatabaseFromManager(database.DatabaseSettings, database.KeyCache);
			database.Migrate();

			// Store the migration version, so we can know it was migrated
			WriteMigrationVersion();
		}

		return database;
	}

	/// <summary>
	/// Reads the version from the migration file.
	/// </summary>
	/// <returns> The last version migrated. </returns>
	protected virtual string ReadMigrationVersion()
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
	/// Writes the current version to the migration file to track migrations.
	/// </summary>
	protected virtual void WriteMigrationVersion()
	{
		ValidateDatabaseName();

		if (!string.IsNullOrWhiteSpace(Version))
		{
			File.WriteAllText(MigrationFilePath, Version);
		}
	}

	IDatabase IDatabaseProvider.GetDatabase()
	{
		return GetDatabase();
	}

	ISyncableDatabase ISyncableDatabaseProvider.GetSyncableDatabase()
	{
		return GetSyncableDatabase();
	}

	private void LoadMigrationStatus()
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

	private void ValidateDatabaseName()
	{
		if (DatabaseName == null)
		{
			throw new InvalidOperationException("Database name is not set and should be!");
		}
	}

	#endregion
}