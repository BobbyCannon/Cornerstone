#region References

using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Sample.Server.Data;

public class SampleServerSyncClientProvider : IServerSyncClientProvider
{
	#region Fields

	private readonly ISyncableDatabaseProvider _databaseProvider;
	private readonly IDateTimeProvider _dateTimeProvider;

	#endregion

	#region Constructors

	public SampleServerSyncClientProvider(ISyncableDatabaseProvider databaseProvider, IDateTimeProvider dateTimeProvider)
	{
		_databaseProvider = databaseProvider;
		_dateTimeProvider = dateTimeProvider;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public ServerSyncClient GetServerSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		return new SampleServerSyncClient(_databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler);
	}

	#endregion
}