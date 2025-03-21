#region References

using System;
using Cornerstone.Runtime;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

public class SyncableDatabaseProvider2<T> : SyncableDatabaseProvider<T>
	where T : ISyncableDatabase
{
	#region Fields

	private readonly Func<DatabaseSettings, DatabaseKeyCache, T> _databaseProvider;

	#endregion

	#region Constructors

	public SyncableDatabaseProvider2(
		Func<DatabaseSettings, DatabaseKeyCache, T> databaseProvider,
		DatabaseKeyCache databaseKeyCache,
		DatabaseSettings defaultDatabaseSettings,
		IDateTimeProvider dateTimeProvider)
		: base(dateTimeProvider, databaseKeyCache, defaultDatabaseSettings)
	{
		_databaseProvider = databaseProvider;
	}

	#endregion

	#region Methods

	protected override T GetDatabaseFromProvider()
	{
		return _databaseProvider.Invoke(Settings, KeyCache);
	}

	/// <inheritdoc />
	protected override T GetDatabaseFromProvider(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return _databaseProvider.Invoke(settings, keyCache);
	}

	#endregion
}

/// <summary>
/// Represents a sync database provider.
/// </summary>
public abstract class SyncableDatabaseProvider<T>
	: DatabaseProvider<T>, ISyncableDatabaseProvider<T>
	where T : ISyncableDatabase
{
	#region Constructors

	protected SyncableDatabaseProvider(
		IDateTimeProvider dateTimeProvider,
		DatabaseKeyCache databaseKeyCache,
		DatabaseSettings databaseSettings)
		: base(dateTimeProvider, databaseSettings)
	{
		KeyCache = databaseKeyCache;
	}

	#endregion

	#region Properties

	public DatabaseKeyCache KeyCache { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public T GetSyncableDatabase()
	{
		return GetDatabase(Settings, KeyCache);
	}

	/// <inheritdoc />
	public T GetSyncableDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return GetDatabase(settings, keyCache);
	}

	/// <inheritdoc />
	ISyncableDatabase ISyncableDatabaseProvider.GetSyncableDatabase()
	{
		return GetSyncableDatabase(Settings, KeyCache);
	}

	/// <inheritdoc />
	ISyncableDatabase ISyncableDatabaseProvider.GetSyncableDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return GetSyncableDatabase(settings, keyCache);
	}

	#endregion
}

/// <summary>
/// Represents a database provider for syncable databases that is also a normal database provider.
/// </summary>
public interface ISyncableDatabaseProvider<out T> : ISyncableDatabaseProvider, IDatabaseProvider<T>
	where T : ISyncableDatabase
{
	#region Methods

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <returns> The database instance. </returns>
	new T GetSyncableDatabase();

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <param name="options"> The database options to use for the new database instance. </param>
	/// <param name="keyCache"> An optional key manager for tracking entity IDs (primary and sync). </param>
	/// <returns> The database instance. </returns>
	new T GetSyncableDatabase(DatabaseSettings options, DatabaseKeyCache keyCache);

	#endregion
}

/// <summary>
/// Represents a database provider for syncable databases.
/// </summary>
public interface ISyncableDatabaseProvider : IDatabaseProvider
{
	#region Properties

	/// <summary>
	/// An optional key manager for tracking entity IDs (primary and sync).
	/// </summary>
	DatabaseKeyCache KeyCache { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <returns> The database instance. </returns>
	ISyncableDatabase GetSyncableDatabase();

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <param name="options"> The database options to use for the new database instance. </param>
	/// <param name="keyCache"> An optional key manager for tracking entity IDs (primary and sync). </param>
	/// <returns> The database instance. </returns>
	ISyncableDatabase GetSyncableDatabase(DatabaseSettings options, DatabaseKeyCache keyCache);

	#endregion
}