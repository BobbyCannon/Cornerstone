#region References

using Cornerstone.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#endregion

namespace Sample.Client.Data.Sql;

public class ClientSqlDatabase : ClientDatabase
{
	#region Constructors

	public ClientSqlDatabase()
	{
		// Default constructor needed for Add-Migration
	}

	public ClientSqlDatabase(DbContextOptions contextOptions, DatabaseSettings settings, DatabaseKeyCache keyCache)
		: base(contextOptions, settings, keyCache)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override string[] SyncOrder => ClientMemoryDatabase.GetSyncOrder();

	#endregion

	#region Methods

	/// <summary>
	/// Update the options for default Cornerstone values. Ex. Migration History will be [system].[MigrationHistory] instead of [dbo].[__EFMigrationsHistory].
	/// </summary>
	/// <param name="builder"> The builder to set the options on. </param>
	public static void UpdateOptions(SqlServerDbContextOptionsBuilder builder)
	{
		builder.MigrationsHistoryTable("MigrationHistory", "system");
	}

	public static ClientSqlDatabase UseSqlServer(string connectionString = null, DatabaseSettings settings = null, DatabaseKeyCache keyCache = null)
	{
		connectionString ??= GetConnectionString();

		var builder = new DbContextOptionsBuilder<ClientDatabase>();
		return new ClientSqlDatabase(builder.UseSqlServer(connectionString, UpdateOptions).Options, settings, keyCache);
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