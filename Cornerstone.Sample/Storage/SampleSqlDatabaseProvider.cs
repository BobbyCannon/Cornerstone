#region References

using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Storage;

public class SampleSqlDatabaseProvider : SyncableDatabaseProvider<ISampleDatabase>
{
	#region Fields

	private readonly string _connectionString;
	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly SqlProvider _provider;

	#endregion

	#region Constructors

	public SampleSqlDatabaseProvider(
		string connectionString,
		SqlProvider provider,
		IDateTimeProvider dateTimeProvider,
		DatabaseKeyCache keyCache = null,
		DatabaseSettings settings = null
	)
		: base(dateTimeProvider, keyCache ?? new DatabaseKeyCache(), settings ?? ISampleDatabase.GetDefaultDatabaseSettings())
	{
		_connectionString = connectionString;
		_dateTimeProvider = dateTimeProvider;
		_provider = provider;
	}

	#endregion

	#region Methods

	protected override ISampleDatabase GetDatabaseFromProvider(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return new SampleSqlDatabase(_connectionString, _provider, settings, keyCache, _dateTimeProvider);
	}

	#endregion
}