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

	protected ServerSyncClient(ISyncableDatabaseProvider provider, IDateTimeProvider timeProvider,
		SyncStatistics syncStatistics, Profiler syncClientProfiler)
		: base("Server", provider, timeProvider, syncStatistics, syncClientProfiler)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc cref="ISyncServerProxy" />
	public sealed override SyncSessionStart BeginSync(Guid id, SyncSettings untrustedSettings)
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

		var syncDevice = new SyncDevice();
		syncDevice.AddOrUpdateSyncClientDetails(untrustedSettings);

		if (!ValidateSyncClient(syncDevice))
		{
			throw new CornerstoneException(Babel.Tower[BabelKeys.SyncClientNotSupported]);
		}

		// Do not allow clients to permanently delete entities
		syncSettings.PermanentDeletions = false;

		return base.BeginSync(id, syncSettings);
	}

	/// <summary>
	/// Ensure the sync client that is connecting to the server is still valid.
	/// </summary>
	/// <param name="syncDevice"> The sync device details. </param>
	/// <returns> True if the sync client is valid otherwise false. </returns>
	protected virtual bool ValidateSyncClient(SyncDevice syncDevice)
	{
		return true;
	}

	#endregion
}