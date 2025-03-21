#region References

using Cornerstone.Profiling;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a provider to get a sync client.
/// </summary>
public interface ISyncClientProvider
{
	#region Methods

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <returns> The database instance. </returns>
	ISyncableDatabase GetSyncableDatabase();

	/// <summary>
	/// Return a client by the provided name and credential.
	/// </summary>
	/// <returns> The sync client. </returns>
	SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler);

	#endregion
}

/// <summary>
/// Represents a provider to get a server sync client.
/// </summary>
public interface IServerSyncClientProvider : ISyncClientProvider
{
	#region Methods

	/// <summary>
	/// Return a client by the provided name and credential.
	/// </summary>
	/// <returns> The sync client. </returns>
	ServerSyncClient GetServerSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler);

	#endregion
}