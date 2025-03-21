#region References

using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Sample.Shared.Storage.Sync;

public class SampleClientSyncClientProvider : ISyncClientProvider
{
	#region Fields

	private readonly ISyncableDatabaseProvider _databaseProvider;
	private readonly IDateTimeProvider _dateTimeProvider;

	private readonly string _name;

	#endregion

	#region Constructors

	public SampleClientSyncClientProvider(string name, ISyncableDatabaseProvider databaseProvider, IDateTimeProvider dateTimeProvider)
	{
		_name = name;
		_databaseProvider = databaseProvider;
		_dateTimeProvider = dateTimeProvider;
	}

	#endregion

	#region Methods

	public ISyncableDatabase GetSyncableDatabase()
	{
		return _databaseProvider.GetSyncableDatabase();
	}

	public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		return new SampleClientSyncClient(_name, _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler);
	}

	#endregion
}