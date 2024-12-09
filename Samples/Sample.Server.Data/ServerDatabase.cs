#region References

using System.IO;
using System.Reflection;
using Cornerstone.EntityFramework;
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

	/// <inheritdoc />
	public ISyncableRepository<AccountEntity, int> Accounts => GetSyncableRepository<AccountEntity, int>();

	/// <inheritdoc />
	public ISyncableRepository<AddressEntity, long> Addresses => GetSyncableRepository<AddressEntity, long>();

	/// <inheritdoc />
	public IRepository<FoodEntity, int> Food => GetRepository<FoodEntity, int>();

	/// <inheritdoc />
	public IRepository<FoodRelationshipEntity, int> FoodRelationships => GetRepository<FoodRelationshipEntity, int>();

	/// <inheritdoc />
	public IRepository<GroupMemberEntity, int> GroupMembers => GetRepository<GroupMemberEntity, int>();

	/// <inheritdoc />
	public IRepository<GroupEntity, int> Groups => GetRepository<GroupEntity, int>();

	/// <inheritdoc />
	public ISyncableRepository<LogEventEntity, long> LogEvents => GetSyncableRepository<LogEventEntity, long>();

	/// <inheritdoc />
	public IRepository<PetEntity, (string Name, int OwnerId)> Pets => GetRepository<PetEntity, (string name, int OwnerId)>();

	/// <inheritdoc />
	public IRepository<PetTypeEntity, string> PetTypes => GetRepository<PetTypeEntity, string>();

	/// <inheritdoc />
	public ISyncableRepository<SettingEntity, long> Settings => GetSyncableRepository<SettingEntity, long>();

	/// <inheritdoc />
	public IRepository<TrackerPathConfigurationEntity, int> TrackerPathConfigurations => GetRepository<TrackerPathConfigurationEntity, int>();

	/// <inheritdoc />
	public IRepository<TrackerPathEntity, long> TrackerPaths => GetRepository<TrackerPathEntity, long>();

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