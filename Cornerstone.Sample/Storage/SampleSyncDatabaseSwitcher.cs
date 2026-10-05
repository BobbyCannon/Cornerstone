#region References

using System;
using System.IO;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Storage;

/// <summary>
/// Swaps the sample sync endpoint between SQLite mapper, EF SQLite, SQL Server mapper, and EF SQL Server.
/// </summary>
public abstract class SampleSyncDatabaseSwitcher : ISyncableDatabaseProvider
{
	#region Fields

	private ISyncableDatabaseProvider _current;
	private readonly IDateTimeProvider _dateTimeProvider;
	private SampleSyncDatabaseKind _kind;
	private readonly string _roleName;
	private readonly SampleSqlDatabaseManager _sqliteMapper;

	#endregion

	#region Constructors

	protected SampleSyncDatabaseSwitcher(
		SampleSqlDatabaseManager sqliteMapper,
		string roleName,
		IDateTimeProvider dateTimeProvider
	)
	{
		_sqliteMapper = sqliteMapper;
		_roleName = roleName;
		_dateTimeProvider = dateTimeProvider;
		_kind = SampleSyncDatabaseKind.SqliteMapper;
		_current = sqliteMapper;
	}

	#endregion

	#region Properties

	public DatabaseKeyCache KeyCache => _current.KeyCache;

	public SampleSyncDatabaseKind Kind => _kind;

	public DatabaseSettings Settings
	{
		get => _current.Settings;
		set => _current.Settings = value;
	}

	public SampleSqlDatabaseManager SqliteMapper => _sqliteMapper;

	#endregion

	#region Methods

	public IDatabase GetDatabase()
	{
		return _current.GetDatabase();
	}

	public IDatabase GetDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return _current.GetDatabase(settings, keyCache);
	}

	public ISyncableDatabase GetSyncableDatabase()
	{
		return _current.GetSyncableDatabase();
	}

	public ISyncableDatabase GetSyncableDatabase(DatabaseSettings options, DatabaseKeyCache keyCache)
	{
		return _current.GetSyncableDatabase(options, keyCache);
	}

	public void Use(SampleSyncDatabaseKind kind)
	{
		if ((_kind == kind) && (_current != null))
		{
			return;
		}

		_current = Create(kind);
		_kind = kind;
	}

	private ISyncableDatabaseProvider Create(SampleSyncDatabaseKind kind)
	{
		switch (kind)
		{
			case SampleSyncDatabaseKind.SqliteMapper:
				return _sqliteMapper;
			case SampleSyncDatabaseKind.EfSqlite:
				return new SampleEntityFrameworkDatabaseProvider(
					$"Data Source={Path.Combine(Path.GetTempPath(), $"Cornerstone.Sample.{_roleName}.ef.db")}",
					SqlProvider.Sqlite,
					_dateTimeProvider
				);
			case SampleSyncDatabaseKind.SqlServerMapper:
				return new SampleSqlDatabaseProvider(
					SqlServerConnectionString(),
					SqlProvider.SqlServer,
					_dateTimeProvider
				);
			case SampleSyncDatabaseKind.EfSqlServer:
				return new SampleEntityFrameworkDatabaseProvider(
					SqlServerConnectionString(),
					SqlProvider.SqlServer,
					_dateTimeProvider
				);
			default:
				throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
		}
	}

	private string SqlServerConnectionString()
	{
		return $"server=localhost;database=Cornerstone.Sample.{_roleName};integrated security=true;encrypt=false;";
	}

	#endregion
}

[SourceReflection]
[DependencyInjected]
public class SampleClientSyncDatabaseSwitcher : SampleSyncDatabaseSwitcher
{
	#region Constructors

	[DependencyInjectionConstructor]
	public SampleClientSyncDatabaseSwitcher(
		SampleClientSqlDatabaseManager sqliteMapper,
		IDateTimeProvider dateTimeProvider
	)
		: base(sqliteMapper, "Client", dateTimeProvider)
	{
	}

	#endregion
}

[SourceReflection]
[DependencyInjected]
public class SampleServerSyncDatabaseSwitcher : SampleSyncDatabaseSwitcher
{
	#region Constructors

	[DependencyInjectionConstructor]
	public SampleServerSyncDatabaseSwitcher(
		SampleServerSqlDatabaseManager sqliteMapper,
		IDateTimeProvider dateTimeProvider
	)
		: base(sqliteMapper, "Server", dateTimeProvider)
	{
	}

	#endregion
}
