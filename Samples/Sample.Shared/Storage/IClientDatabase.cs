#region References

using System;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage.Client;

#endregion

namespace Sample.Shared.Storage;

public interface IClientDatabase : ISyncableDatabase, ITrackerDatabase
{
	#region Fields

	private static string[] _syncOrder;

	#endregion

	#region Properties

	ISyncableRepository<ClientAccount, int> Accounts { get; }

	ISyncableRepository<ClientAddress, long> Addresses { get; }

	ISyncableRepository<ClientLogEvent, long> LogEvents { get; }

	ISyncableRepository<ClientSetting, long> Settings { get; }

	#endregion

	#region Methods

	public static DatabaseSettings GetDefaultDatabaseSettings()
	{
		return new DatabaseSettings { SyncOrder = GetSyncOrder() };
	}

	public static string[] GetSyncOrder()
	{
		return _syncOrder ??= GetSyncTypes().Select(x => x.ToAssemblyName()).ToArray();
	}

	public static Type[] GetSyncTypes()
	{
		return
		[
			typeof(TrackerPathConfigurationEntity),
			typeof(TrackerPathEntity),
			typeof(ClientAccount),
			typeof(ClientAddress),
			typeof(ClientLogEvent),
			typeof(ClientSetting)
		];
	}

	#endregion
}