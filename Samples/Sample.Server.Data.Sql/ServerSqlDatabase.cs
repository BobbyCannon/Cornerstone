#region References

using Cornerstone.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#endregion

namespace Sample.Server.Data.Sql;

public class ServerSqlDatabase : ServerDatabase
{
	#region Constructors

	public ServerSqlDatabase()
	{
		// Default constructor needed for Add-Migration
	}

	public ServerSqlDatabase(DbContextOptions contextOptions, DatabaseSettings settings, DatabaseKeyCache keyCache)
		: base(contextOptions, settings, keyCache)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override string[] SyncOrder => ServerMemoryDatabase.GetSyncOrder();

	#endregion

	#region Methods

	/// <summary>
	/// Update the options for default Speedy values. Ex. Migration History will be [system].[MigrationHistory] instead of [dbo].[__EFMigrationsHistory].
	/// </summary>
	/// <param name="builder"> The builder to set the options on. </param>
	public static void UpdateOptions(SqlServerDbContextOptionsBuilder builder)
	{
		builder.MigrationsHistoryTable("MigrationHistory", "system");
	}

	public static ServerSqlDatabase UseSqlServer(string connectionString = null, DatabaseSettings settings = null, DatabaseKeyCache keyCache = null)
	{
		connectionString ??= GetConnectionString();

		var builder = new DbContextOptionsBuilder<ServerDatabase>();
		return new ServerSqlDatabase(builder.UseSqlServer(connectionString, UpdateOptions).Options, settings, keyCache);
	}

	protected override void ConfigureDatabaseOptions(DbContextOptionsBuilder options)
	{
		if (!options.IsConfigured)
		{
			options.UseSqlServer(GetConnectionString(), UpdateOptions);
		}

		base.ConfigureDatabaseOptions(options);
	}

	#endregion
}