#region References

using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Website.Sync;

public class SampleServerSyncClientProvider : IServerSyncClientProvider
{
	#region Fields

	private readonly AccountEntity _account;
	private readonly ISyncableDatabaseProvider _databaseProvider;
	private readonly IDateTimeProvider _dateTimeProvider;

	private readonly string _name;

	#endregion

	#region Constructors

	public SampleServerSyncClientProvider(string name, AccountEntity account,
		ISyncableDatabaseProvider databaseProvider,
		IDateTimeProvider dateTimeProvider)
	{
		_name = name;
		_account = account;
		_databaseProvider = databaseProvider;
		_dateTimeProvider = dateTimeProvider;
	}

	#endregion

	#region Methods

	public ServerSyncClient GetServerSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		return new SampleServerSyncClient(_name, _account, _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler);
	}

	public ISyncableDatabase GetSyncableDatabase()
	{
		return _databaseProvider.GetSyncableDatabase();
	}

	public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		return GetServerSyncClient(syncStatistics, syncClientProfiler);
	}

	#endregion
}