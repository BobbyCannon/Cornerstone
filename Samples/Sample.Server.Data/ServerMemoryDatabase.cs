#region References

using Cornerstone.EntityFramework;
using Cornerstone.Logging;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Data;

public class ServerMemoryDatabase : SyncableDatabase, IServerDatabase
{
	#region Constructors

	/// <inheritdoc />
	public ServerMemoryDatabase() : this(null, null, null)
	{
	}

	/// <inheritdoc />
	public ServerMemoryDatabase(IDateTimeProvider dateTimeProvider, DatabaseSettings settings, DatabaseKeyCache keyCache)
		: base(dateTimeProvider, settings, keyCache)
	{
		Accounts = GetSyncableRepository<AccountEntity, int>();
		Addresses = GetSyncableRepository<AddressEntity, long>();
		Food = GetRepository<FoodEntity, int>();
		FoodRelationships = GetRepository<FoodRelationshipEntity, int>();
		GroupMembers = GetRepository<GroupMemberEntity, int>();
		Groups = GetRepository<GroupEntity, int>();
		LogEvents = GetSyncableRepository<LogEventEntity, long>();
		Pets = GetRepository<PetEntity, (string Name, int OwnerId)>();
		PetTypes = GetRepository<PetTypeEntity, string>();
		Settings = GetSyncableRepository<SettingEntity, long>();
		TrackerPathConfigurations = GetSyncableRepository<TrackerPathConfigurationEntity, int>();
		TrackerPaths = GetSyncableRepository<TrackerPathEntity, long>();

		this.ConfigureModelViaMapping();
	}

	#endregion

	#region Properties

	public ISyncableRepository<AccountEntity, int> Accounts { get; }

	public ISyncableRepository<AddressEntity, long> Addresses { get; }

	public IRepository<FoodEntity, int> Food { get; }

	public IRepository<FoodRelationshipEntity, int> FoodRelationships { get; }

	public IRepository<GroupMemberEntity, int> GroupMembers { get; }

	public IRepository<GroupEntity, int> Groups { get; }

	public ISyncableRepository<LogEventEntity, long> LogEvents { get; }

	public IRepository<PetEntity, (string Name, int OwnerId)> Pets { get; }

	public IRepository<PetTypeEntity, string> PetTypes { get; }

	public ISyncableRepository<SettingEntity, long> Settings { get; }

	public override string[] SyncOrder => IServerDatabase.GetSyncOrder();

	public ISyncableRepository<TrackerPathConfigurationEntity, int> TrackerPathConfigurations { get; }

	public ISyncableRepository<TrackerPathEntity, long> TrackerPaths { get; }

	#endregion
}