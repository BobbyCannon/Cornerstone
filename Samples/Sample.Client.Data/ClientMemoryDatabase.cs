#region References

using Cornerstone.EntityFramework;
using Cornerstone.Logging;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Client;

#endregion

namespace Sample.Client.Data;

public class ClientMemoryDatabase : SyncableDatabase, IClientDatabase
{
	#region Constructors

	/// <inheritdoc />
	public ClientMemoryDatabase() : this(null, null, null)
	{
	}

	/// <inheritdoc />
	public ClientMemoryDatabase(IDateTimeProvider dateTimeProvider, DatabaseSettings settings, DatabaseKeyCache keyCache)
		: base(dateTimeProvider, settings, keyCache)
	{
		Accounts = GetSyncableRepository<ClientAccount, int>();
		Addresses = GetSyncableRepository<ClientAddress, long>();
		LogEvents = GetSyncableRepository<ClientLogEvent, long>();
		Settings = GetSyncableRepository<ClientSetting, long>();
		TrackerPathConfigurations = GetSyncableRepository<TrackerPathConfigurationEntity, int>();
		TrackerPaths = GetSyncableRepository<TrackerPathEntity, long>();

		this.ConfigureModelViaMapping();
	}

	#endregion

	#region Properties

	public ISyncableRepository<ClientAccount, int> Accounts { get; }

	public ISyncableRepository<ClientAddress, long> Addresses { get; }

	public ISyncableRepository<ClientLogEvent, long> LogEvents { get; }

	public ISyncableRepository<ClientSetting, long> Settings { get; }

	public override string[] SyncOrder => IClientDatabase.GetSyncOrder();

	public ISyncableRepository<TrackerPathConfigurationEntity, int> TrackerPathConfigurations { get; }

	public ISyncableRepository<TrackerPathEntity, long> TrackerPaths { get; }

	#endregion
}