#region References

using System;
using System.IO;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Handles management of a syncable database for application client database.
/// </summary>
/// <typeparam name="T"> The type that represents the database </typeparam>
public abstract class DatabaseManagerOptimizedMigrations<T>
	: DatabaseManager<T>
	where T : ISyncableDatabase
{
	#region Fields

	private string _databaseFilePath, _migrationFilePath;

	#endregion

	#region Constructors

	protected DatabaseManagerOptimizedMigrations(
		DatabaseKeyCache databaseKeyCache,
		DatabaseSettings databaseSettings,
		IRuntimeInformation runtimeInformation,
		IDispatcher dispatcher
	) : base(databaseKeyCache, databaseSettings, dispatcher)
	{
		DatabaseName = runtimeInformation.ApplicationName;
		StoragePath = runtimeInformation.ApplicationDataLocation;
		Version = runtimeInformation.ApplicationVersion.ToString(4);
	}

	#endregion

	#region Properties

	public string DatabaseFilePath => _databaseFilePath ??= Path.Combine(StoragePath, $"{DatabaseName}.db");

	public string DatabaseName { get; }

	public bool IsMigrated { get; private set; }

	public string MigrationFilePath => _migrationFilePath ??= Path.Combine(StoragePath, $"{DatabaseName}.db.version");

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
	public override void Initialize()
	{
		if (IsInitialized)
		{
			return;
		}

		// Refresh the migration status
		new DirectoryInfo(StoragePath).SafeCreate();
		LoadMigrationStatus();
		base.Initialize();
	}

	/// <inheritdoc />
	public override void Uninitialize()
	{
		IsMigrated = false;

		_databaseFilePath = null;
		_migrationFilePath = null;

		KeyCache?.Clear();
	}

	protected string GetConnectionString()
	{
		ValidateDatabaseName();
		return $"Data Source={DatabaseFilePath}";
	}

	protected override T Migrate(T database, bool canRetry)
	{
		var response = base.Migrate(database, canRetry);
		IsMigrated = true;
		return response;
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

	protected override bool ShouldMigrate()
	{
		return !IsMigrated;
	}

	/// <summary>
	/// Writes the current version to the migration file to track migrations.
	/// </summary>
	protected override void WriteMigrationVersion()
	{
		ValidateDatabaseName();

		if (!string.IsNullOrWhiteSpace(Version))
		{
			File.WriteAllText(MigrationFilePath, Version);
		}
	}

	private void LoadMigrationStatus()
	{
		try
		{
			IsMigrated = Version == ReadMigrationVersion();
		}
		catch
		{
			IsMigrated = false;
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