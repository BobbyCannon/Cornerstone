#region References

using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync database provider.
/// </summary>
public abstract class SyncableDatabaseProvider<T>
	: DatabaseProvider<T>, ISyncableDatabaseProvider<T>
	where T : ISyncableDatabase
{
	#region Constructors

	/// <inheritdoc />
	protected SyncableDatabaseProvider(DatabaseOptions options) : base(options)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public T GetSyncableDatabase()
	{
		return GetDatabase();
	}

	/// <inheritdoc />
	ISyncableDatabase ISyncableDatabaseProvider.GetSyncableDatabase()
	{
		return GetSyncableDatabase();
	}

	#endregion
}

/// <summary>
/// Represents a database provider for syncable databases.
/// </summary>
public interface ISyncableDatabaseProvider<out T>
	: IDatabaseProvider<T>, ISyncableDatabaseProvider
	where T : ISyncableDatabase
{
	#region Methods

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <returns> The database instance. </returns>
	new T GetSyncableDatabase();

	#endregion
}

/// <summary>
/// Represents a database provider for syncable databases.
/// </summary>
public interface ISyncableDatabaseProvider : IDatabaseProvider
{
	#region Methods

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <returns> The database instance. </returns>
	ISyncableDatabase GetSyncableDatabase();

	#endregion
}