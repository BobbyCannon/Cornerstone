﻿#region References

using System.IO;
using System.Reflection;
using Cornerstone.EntityFramework;
using Cornerstone.Logging;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sample.Client.Data.Mappings;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Client;
using ConfigurationManager = System.Configuration.ConfigurationManager;

#endregion

namespace Sample.Client.Data;

public abstract class ClientDatabase : EntityFrameworkSyncableDatabase, IClientDatabase
{
	#region Constructors

	protected ClientDatabase()
	{
		// Default constructor needed for Add-Migration
	}

	protected ClientDatabase(DbContextOptions contextOptions, DatabaseSettings settings, DatabaseKeyCache keyCache)
		: base(contextOptions, settings, keyCache)
	{
	}

	#endregion

	#region Properties

	public ISyncableRepository<ClientAccount, int> Accounts => GetSyncableRepository<ClientAccount, int>();
	public ISyncableRepository<ClientAddress, long> Addresses => GetSyncableRepository<ClientAddress, long>();
	public ISyncableRepository<ClientLogEvent, long> LogEvents => GetSyncableRepository<ClientLogEvent, long>();
	public ISyncableRepository<ClientSetting, long> Settings => GetSyncableRepository<ClientSetting, long>();
	public override string[] SyncOrder => IClientDatabase.GetSyncOrder();
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
		return typeof(ClientAccountMap).Assembly;
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