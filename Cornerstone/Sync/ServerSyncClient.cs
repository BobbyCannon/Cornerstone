#region References

using System;
using Cornerstone.Exceptions;
using Cornerstone.Profiling;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

public abstract class ServerSyncClient : SyncClient
{
	#region Constructors

	protected ServerSyncClient(
		string name,
		ISyncableDatabaseProvider databaseProvider,
		IDateTimeProvider dateTimeProvider,
		SyncStatistics syncStatistics,
		Profiler syncClientProfiler
	) : base(name, databaseProvider, dateTimeProvider, syncStatistics, syncClientProfiler)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc cref="ISyncServerProxy" />
	public override SyncSessionStart BeginSync(Guid id, SyncSettings untrustedSettings)
	{
		// note: never trust the sync options. These are just suggestions from the client, you MUST ensure these suggestions are valid.
		var syncSettings = new SyncSettings
		{
			IncludeIssueDetails = false,
			ItemsPerSyncRequest = untrustedSettings.ItemsPerSyncRequest > 10000 ? 10000 : untrustedSettings.ItemsPerSyncRequest,
			// Do not allow clients to permanently delete entities
			PermanentDeletions = false,
			LastSyncedOnClient = untrustedSettings.LastSyncedOnClient,
			LastSyncedOnServer = untrustedSettings.LastSyncedOnServer,
			SyncType = untrustedSettings.SyncType,
			SyncDirection = untrustedSettings.SyncDirection
		};

		var response = base.BeginSync(id, syncSettings);

		if (!ValidateSyncClient())
		{
			throw new CornerstoneException(Babel.Tower[BabelKeys.SyncClientNotSupported]);
		}

		// Do not allow clients to permanently delete entities
		syncSettings.PermanentDeletions = false;

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