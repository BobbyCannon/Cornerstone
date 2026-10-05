#region References

using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Cornerstone.Sample.Storage;

public class SampleEntityFrameworkDatabaseProvider : SyncableDatabaseProvider<ISampleDatabase>
{
	#region Fields

	private readonly string _connectionString;
	private readonly SqlProvider _provider;

	#endregion

	#region Constructors

	public SampleEntityFrameworkDatabaseProvider(
		string connectionString,
		SqlProvider provider,
		IDateTimeProvider dateTimeProvider,
		DatabaseKeyCache keyCache = null,
		DatabaseSettings settings = null
	)
		: base(dateTimeProvider, keyCache, settings ?? ISampleDatabase.GetDefaultDatabaseSettings())
	{
		_connectionString = connectionString;
		_provider = provider;
	}

	#endregion

	#region Methods

	protected override ISampleDatabase GetDatabaseFromProvider(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		var builder = new DbContextOptionsBuilder<SampleEntityFrameworkDatabase>();
		if (_provider == SqlProvider.SqlServer)
		{
			builder.UseSqlServer(_connectionString);
		}
		else
		{
			builder.UseSqlite(_connectionString);
		}

		return new SampleEntityFrameworkDatabase(builder.Options, settings, keyCache);
	}

	#endregion
}
