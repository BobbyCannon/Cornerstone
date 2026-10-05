#region References

using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Sync;

/// <summary>
/// Builds the sync client for one side of a mesh link.
/// A hub server is <see cref="SampleServerSyncClient"/>. A peer is <see cref="SampleSyncClient"/>.
/// </summary>
public class MeshSyncClientProvider : ISyncClientProvider
{
	#region Fields

	private readonly ISyncableDatabaseProvider _databaseProvider;
	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly bool _hub;
	private readonly string _name;

	#endregion

	#region Constructors

	public MeshSyncClientProvider(
		string name,
		ISyncableDatabaseProvider databaseProvider,
		IDateTimeProvider dateTimeProvider,
		bool hub
	)
	{
		_name = name;
		_databaseProvider = databaseProvider;
		_dateTimeProvider = dateTimeProvider;
		_hub = hub;
	}

	#endregion

	#region Methods

	public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		if (_hub)
		{
			return new SampleServerSyncClient(_name, _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler);
		}

		return new SampleSyncClient(_name, _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler);
	}

	public ISyncableDatabase GetSyncableDatabase()
	{
		return _databaseProvider.GetSyncableDatabase();
	}

	#endregion
}
