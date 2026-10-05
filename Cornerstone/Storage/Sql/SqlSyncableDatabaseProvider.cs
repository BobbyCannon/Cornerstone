#region References

using System;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Storage.Sql;

/// <summary>
/// Provides <see cref="SqlSyncableDatabase" /> instances for sync. Use with
/// <see cref="SyncClientForDatabase" /> in place of an EF database provider.
/// </summary>
public class SqlSyncableDatabaseProvider : SyncableDatabaseProvider<SqlSyncableDatabase>
{
	#region Fields

	private readonly Type[] _entityTypes;

	#endregion

	#region Constructors

	public SqlSyncableDatabaseProvider(
		string connectionString,
		SqlProvider provider,
		IDateTimeProvider dateTimeProvider,
		params Type[] entityTypes
	)
		: this(connectionString, provider, dateTimeProvider, null, null, entityTypes)
	{
	}

	public SqlSyncableDatabaseProvider(
		string connectionString,
		SqlProvider provider,
		IDateTimeProvider dateTimeProvider,
		DatabaseKeyCache keyCache,
		DatabaseSettings settings,
		params Type[] entityTypes
	)
		: base(dateTimeProvider, keyCache, settings ?? new DatabaseSettings())
	{
		ConnectionString = connectionString;
		_entityTypes = entityTypes ?? [];
		Provider = provider;
	}

	#endregion

	#region Properties

	public string ConnectionString { get; }

	public SqlProvider Provider { get; }

	#endregion

	#region Methods

	protected override SqlSyncableDatabase GetDatabaseFromProvider(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return new SqlSyncableDatabase(ConnectionString, Provider, settings, keyCache, null, _entityTypes);
	}

	#endregion
}