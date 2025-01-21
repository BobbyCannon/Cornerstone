#region References

using System.IO;
using System.Reflection;
using Cornerstone.EntityFramework;
using Cornerstone.Logging;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sample.Server.Data.Mappings;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Server;
using ConfigurationManager = System.Configuration.ConfigurationManager;

#endregion

namespace Sample.Server.Data;

public abstract class ServerDatabase : EntityFrameworkSyncableDatabase, IServerDatabase
{
	#region Constructors

	public ServerDatabase()
	{
		// Default constructor needed for Add-Migration
	}

	protected ServerDatabase(DbContextOptions contextOptions, DatabaseSettings settings, DatabaseKeyCache keyCache)
		: base(contextOptions, settings, keyCache)
	{
	}

	#endregion

	#region Properties

	public ISyncableRepository<AccountEntity, int> Accounts => GetSyncableRepository<AccountEntity, int>();
	public ISyncableRepository<AddressEntity, long> Addresses => GetSyncableRepository<AddressEntity, long>();
	public IRepository<FoodEntity, int> Food => GetRepository<FoodEntity, int>();
	public IRepository<FoodRelationshipEntity, int> FoodRelationships => GetRepository<FoodRelationshipEntity, int>();
	public IRepository<GroupMemberEntity, int> GroupMembers => GetRepository<GroupMemberEntity, int>();
	public IRepository<GroupEntity, int> Groups => GetRepository<GroupEntity, int>();
	public ISyncableRepository<LogEventEntity, long> LogEvents => GetSyncableRepository<LogEventEntity, long>();
	public IRepository<PetEntity, (string Name, int OwnerId)> Pets => GetRepository<PetEntity, (string name, int OwnerId)>();
	public IRepository<PetTypeEntity, string> PetTypes => GetRepository<PetTypeEntity, string>();
	public ISyncableRepository<SettingEntity, long> Settings => GetSyncableRepository<SettingEntity, long>();
	public ISyncableRepository<TrackerPathConfigurationEntity, int> TrackerPathConfigurations => GetSyncableRepository<TrackerPathConfigurationEntity, int>();
	public ISyncableRepository<TrackerPathEntity, long> TrackerPaths => GetSyncableRepository<TrackerPathEntity, long>();

	#endregion

	#region Methods

	public static string GetConnectionString()
	{
		var connection = ConfigurationManager.ConnectionStrings["DefaultConnection"];
		if ((connection != null) && !string.IsNullOrWhiteSpace(connection.ConnectionString))
		{
			return connection.ConnectionString;
		}

		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("AppSettings.json", true)
			.Build();

		return configuration.GetConnectionString("DefaultConnection");
	}

	/// <inheritdoc />
	public override Assembly GetMappingAssembly()
	{
		return typeof(AccountMap).Assembly;
	}

	protected virtual void ConfigureDatabaseOptions(DbContextOptionsBuilder options)
	{
		options.UseLazyLoadingProxies();
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		ConfigureDatabaseOptions(optionsBuilder);
		base.OnConfiguring(optionsBuilder);
	}

	#endregion
}