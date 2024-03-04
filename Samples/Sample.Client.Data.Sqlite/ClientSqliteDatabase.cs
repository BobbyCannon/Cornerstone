#region References

using Cornerstone.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#endregion

namespace Sample.Client.Data.Sqlite;

public class ClientSqliteDatabase : ClientDatabase
{
	#region Constructors

	public ClientSqliteDatabase()
	{
		// Default constructor needed for Add-Migration
	}

	public ClientSqliteDatabase(DbContextOptions<ClientDatabase> contextOptions, DatabaseOptions options, DatabaseKeyCache keyCache)
		: base(contextOptions, options, keyCache)
	{
	}

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

	public static ClientSqliteDatabase UseSqlite(string connectionString = null, DatabaseOptions options = null, DatabaseKeyCache keyCache = null)
	{
		connectionString ??= GetConnectionString();

		var builder = new DbContextOptionsBuilder<ClientDatabase>();
		return new ClientSqliteDatabase(builder.UseSqlite(connectionString, UpdateOptions).Options, options, keyCache);
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