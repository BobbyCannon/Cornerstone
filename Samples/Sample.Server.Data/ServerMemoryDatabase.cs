#region References

using Cornerstone.EntityFramework;
using Cornerstone.Extensions;
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
	public ServerMemoryDatabase() : this(null, null)
	{
	}

	/// <inheritdoc />
	public ServerMemoryDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache) : base(settings, keyCache)
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

	/// <inheritdoc />
	public ISyncableRepository<AccountEntity, int> Accounts { get; }

	/// <inheritdoc />
	public ISyncableRepository<AddressEntity, long> Addresses { get; }

	/// <inheritdoc />
	public IRepository<FoodEntity, int> Food { get; }

	/// <inheritdoc />
	public IRepository<FoodRelationshipEntity, int> FoodRelationships { get; }

	/// <inheritdoc />
	public IRepository<GroupMemberEntity, int> GroupMembers { get; }

	/// <inheritdoc />
	public IRepository<GroupEntity, int> Groups { get; }

	/// <inheritdoc />
	public ISyncableRepository<LogEventEntity, long> LogEvents { get; }

	/// <inheritdoc />
	public IRepository<PetEntity, (string Name, int OwnerId)> Pets { get; }

	/// <inheritdoc />
	public IRepository<PetTypeEntity, string> PetTypes { get; }

	/// <inheritdoc />
	public ISyncableRepository<SettingEntity, long> Settings { get; }

	/// <inheritdoc />
	public override string[] SyncOrder => GetSyncOrder();

	/// <inheritdoc />
	public IRepository<TrackerPathConfigurationEntity, int> TrackerPathConfigurations { get; }

	/// <inheritdoc />
	public IRepository<TrackerPathEntity, long> TrackerPaths { get; }

	#endregion

	#region Methods

	public static string[] GetSyncOrder()
	{
		return
		[
			typeof(AccountEntity).ToAssemblyName(),
			typeof(AddressEntity).ToAssemblyName(),
			typeof(LogEventEntity).ToAssemblyName(),
			typeof(SettingEntity).ToAssemblyName()
		];
	}

	#endregion
}