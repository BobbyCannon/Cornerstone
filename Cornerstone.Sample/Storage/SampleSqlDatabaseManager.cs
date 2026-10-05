#region References

using System;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Storage.Migrations;
using Cornerstone.Storage;
using Cornerstone.Storage.Sql;

#endregion

namespace Cornerstone.Sample.Storage;

/// <summary>
/// App-facing provider for <see cref="Migrations.SampleSqlDatabase" />. Opens a database and migrates when needed.
/// </summary>
public class SampleSqlDatabaseManager : DatabaseManager<ISampleDatabase>
{
	#region Fields

	private readonly bool _isMemoryDatabase;
	private ISampleDatabase _memoryKeepAlive;
	private readonly SqlProvider _provider;

	#endregion

	#region Constructors

	public SampleSqlDatabaseManager(
		IDateTimeProvider dateTimeProvider,
		Profiler profiler,
		string connectionString,
		SqlProvider provider
	)
		: base(
			dateTimeProvider,
			new DatabaseKeyCache(TimeSpan.FromMinutes(5)),
			ISampleDatabase.GetDefaultDatabaseSettings(),
			profiler ?? new Profiler("SampleSql")
		)
	{
		_memoryKeepAlive = null;
		_provider = provider;
		ConnectionString = connectionString;
		_isMemoryDatabase = connectionString.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase);
	}

	#endregion

	#region Methods

	protected override ISampleDatabase GetDatabaseFromManager(
		DatabaseSettings settings,
		DatabaseKeyCache keyCache,
		IDateTimeProvider dateTimeProvider)
	{
		if (_isMemoryDatabase)
		{
			_memoryKeepAlive ??= new SampleSqlDatabase(ConnectionString, _provider, settings, keyCache, dateTimeProvider);
		}

		return new SampleSqlDatabase(ConnectionString, _provider, settings, keyCache, dateTimeProvider);
	}

	public override void UninitializeLifecycle()
	{
		_memoryKeepAlive?.Dispose();
		_memoryKeepAlive = null;
		base.UninitializeLifecycle();
	}

	public static string GetSqliteConnectionString(IRuntimeInformation runtimeInformation, string fileName)
	{
		// Sample is a demo: always shared in-memory so desktop cannot reuse a stale
		// file schema (CREATE TABLE IF NOT EXISTS will not add columns) and browser
		// has no persistent SQLite file. fileName keeps client and server apart.
		_ = runtimeInformation;
		return $"Data Source={fileName};Mode=Memory;Cache=Shared;";
	}

	#endregion
}
