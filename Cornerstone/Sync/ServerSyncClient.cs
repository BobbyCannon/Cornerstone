#region References

using System;
using Cornerstone.Logging;
using Cornerstone.Profiling;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

public abstract class ServerSyncClient : SyncClientForDatabase
{
	#region Constructors

	protected ServerSyncClient(
		string name,
		ISyncableDatabaseProvider databaseProvider,
		IDateTimeProvider dateTimeProvider,
		SyncStatistics syncStatistics,
		Profiler syncClientProfiler,
		Logger logger = null
	) : base(name, databaseProvider, dateTimeProvider, syncStatistics, syncClientProfiler, logger)
	{
	}

	#endregion

	#region Methods

	protected internal override SyncSessionStart BeginSync(Guid id, SyncSettings untrustedSettings)
	{
		// note: never trust the sync options. These are just suggestions from the client, you MUST ensure these suggestions are valid.
		untrustedSettings.IncludeIssueDetails = false;
		untrustedSettings.ItemsPerSyncRequest = untrustedSettings.ItemsPerSyncRequest switch
		{
			> 10000 => 10000,
			<= 0 => 1,
			_ => untrustedSettings.ItemsPerSyncRequest
		};
		untrustedSettings.PermanentDeletions = false;

		var response = base.BeginSync(id, untrustedSettings);

		if (!ValidateSyncClient())
		{
			throw new CornerstoneException(Babel.Tower[BabelKeys.SyncClientNotSupported]);
		}

		untrustedSettings.PermanentDeletions = false;

		return response;
	}

	/// <summary>
	/// Ensure the sync client that is connecting to the server is still valid.
	/// </summary>
	/// <returns> True if the sync client is valid otherwise false. </returns>
	protected virtual bool ValidateSyncClient()
	{
		return true;
	}

	#endregion
}