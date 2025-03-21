#region References

using System;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Shared.Storage;

public interface IServerDatabase : ISyncableDatabase, ITrackerDatabase
{
	#region Fields

	private static string[] _syncOrder;

	#endregion

	#region Properties

	ISyncableRepository<AccountEntity, int> Accounts { get; }
	ISyncableRepository<AddressEntity, long> Addresses { get; }
	IRepository<FoodEntity, int> Food { get; }
	IRepository<FoodRelationshipEntity, int> FoodRelationships { get; }
	IRepository<GroupMemberEntity, int> GroupMembers { get; }
	IRepository<GroupEntity, int> Groups { get; }
	ISyncableRepository<LogEventEntity, long> LogEvents { get; }
	IRepository<PetEntity, (string Name, int OwnerId)> Pets { get; }
	IRepository<PetTypeEntity, string> PetTypes { get; }
	ISyncableRepository<SettingEntity, long> Settings { get; }

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
			typeof(AddressEntity),
			typeof(SettingEntity),
			typeof(AccountEntity),
			typeof(LogEventEntity)
		];
	}

	#endregion
}