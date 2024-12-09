#region References

using System;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

public class SyncableDatabaseProvider2<T> : SyncableDatabaseProvider<T>
	where T : ISyncableDatabase
{
	#region Fields

	private readonly Func<T> _databaseProvider;

	#endregion

	#region Constructors

	public SyncableDatabaseProvider2(Func<T> databaseProvider)
	{
		_databaseProvider = databaseProvider;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override T GetDatabaseFromProvider()
	{
		return _databaseProvider.Invoke();
	}

	#endregion

	/// <inheritdoc />
	public override string[] GetSyncOrder()
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Represents a sync database provider.
/// </summary>
public abstract class SyncableDatabaseProvider<T>
	: DatabaseProvider<T>, ISyncableDatabaseProvider<T>
	where T : ISyncableDatabase
{
	#region Methods

	/// <inheritdoc />
	public T GetSyncableDatabase()
	{
		return GetDatabase();
	}

	/// <inheritdoc />
	public abstract string[] GetSyncOrder();

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

	/// <summary>
	/// Gets the order in which to sync entities.
	/// </summary>
	/// <returns> </returns>
	string[] GetSyncOrder();

	#endregion
}