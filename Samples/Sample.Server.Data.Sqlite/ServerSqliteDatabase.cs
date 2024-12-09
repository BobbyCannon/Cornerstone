#region References

using Cornerstone.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#endregion

namespace Sample.Server.Data.Sqlite;

public class ServerSqliteDatabase : ServerDatabase
{
	#region Constructors

	public ServerSqliteDatabase()
	{
		// Default constructor needed for Add-Migration
	}

	public ServerSqliteDatabase(DbContextOptions<ServerDatabase> contextOptions, DatabaseSettings settings, DatabaseKeyCache keyCache)
		: base(contextOptions, settings, keyCache)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override string[] SyncOrder { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the options for default Speedy values. Ex. Migration History will be [system].[MigrationHistory] instead of [dbo].[__EFMigrationsHistory].
	/// </summary>
	/// <param name="builder"> The builder to set the options on. </param>
	public static void UpdateOptions(SqliteDbContextOptionsBuilder builder)
	{
		builder.MigrationsHistoryTable("MigrationHistory", "system");
	}

	public static ServerSqliteDatabase UseSqlite(string connectionString = null, DatabaseSettings settings = null, DatabaseKeyCache keyCache = null)
	{
		connectionString ??= GetConnectionString();

		var builder = new DbContextOptionsBuilder<ServerDatabase>();
		return new ServerSqliteDatabase(builder.UseSqlite(connectionString, UpdateOptions).Options, settings, keyCache);
	}

	protected override void ConfigureDatabaseOptions(DbContextOptionsBuilder options)
	{
		if (!options.IsConfigured)
		{
			options.UseSqlite(GetConnectionString(), UpdateOptions);
		}

		base.ConfigureDatabaseOptions(options);
	}

	#endregion
}