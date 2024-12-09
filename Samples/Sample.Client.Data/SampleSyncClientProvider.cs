#region References

using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Sample.Client.Data;

public class SampleSyncClientProvider : ISyncClientProvider
{
	#region Fields

	private readonly ISyncableDatabaseProvider _databaseProvider;
	private readonly IDateTimeProvider _dateTimeProvider;

	#endregion

	#region Constructors

	public SampleSyncClientProvider(ISyncableDatabaseProvider databaseProvider, IDateTimeProvider dateTimeProvider)
	{
		_databaseProvider = databaseProvider;
		_dateTimeProvider = dateTimeProvider;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		return new SampleSyncClient(_databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler);
	}

	#endregion
}