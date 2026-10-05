#region References

using System.Linq;
using Cornerstone.Sample.Storage;
using Cornerstone.Storage.Sql;
using Cornerstone.Storage.Sql.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Storage;

[TestClass]
[DoNotParallelize]
public class SqlDatabaseEnsureCreatedTests
{
	#region Methods

	[TestMethod]
	public void EnsureDatabaseCreatedSqlServer()
	{
		var connectionString = SqlProviderHarness.SqlServerConnectionString();
		SqlProviderHarness.RequireSqlServer(connectionString);
		SqlProviderHarness.DropSqlServerDatabase(connectionString);
		Assert.IsFalse(SqlProviderHarness.IsDatabasePresent(SqlProvider.SqlServer, connectionString));

		try
		{
			using var database = new SqlDatabase(connectionString, SqlProvider.SqlServer);
			database.EnsureDatabaseCreated();
			Assert.IsTrue(SqlProviderHarness.IsDatabasePresent(SqlProvider.SqlServer, connectionString));
		}
		finally
		{
			SqlProviderHarness.DropSqlServerDatabase(connectionString);
			Assert.IsFalse(SqlProviderHarness.IsDatabasePresent(SqlProvider.SqlServer, connectionString));
		}
	}

	[TestMethod]
	public void EnsureDatabaseCreatedSqlServerIsIdempotent()
	{
		var connectionString = SqlProviderHarness.SqlServerConnectionString();
		SqlProviderHarness.RequireSqlServer(connectionString);
		SqlProviderHarness.DropSqlServerDatabase(connectionString);

		try
		{
			using var database = new SqlDatabase(connectionString, SqlProvider.SqlServer);
			database.EnsureDatabaseCreated();
			database.EnsureDatabaseCreated();
			Assert.IsTrue(SqlProviderHarness.IsDatabasePresent(SqlProvider.SqlServer, connectionString));
		}
		finally
		{
			SqlProviderHarness.DropSqlServerDatabase(connectionString);
		}
	}

	[TestMethod]
	public void EnsureDatabaseCreatedSqliteMemory()
	{
		var connectionString = SqlProviderHarness.CreateSqliteMemoryConnectionString();
		using var database = new SqlDatabase(connectionString, SqlProvider.Sqlite);
		database.EnsureDatabaseCreated();
		Assert.IsTrue(SqlProviderHarness.IsDatabasePresent(SqlProvider.Sqlite, connectionString));
	}

	[TestMethod]
	public void MigrateSampleSqlServer()
	{
		var connectionString = SqlProviderHarness.SqlServerConnectionString();
		SqlProviderHarness.RequireSqlServer(connectionString);
		SqlProviderHarness.DropSqlServerDatabase(connectionString);

		try
		{
			using var database = new SampleSqlDatabase(connectionString, SqlProvider.SqlServer, null, null, null);
			database.EnsureDatabaseCreated();
			database.Migrate();
			Assert.IsTrue(database.IsDatabaseMigrated());
			var names = database.QueryTables().Select(x => x.Name).ToList();
			CollectionAssert.Contains(names, "Accounts");
			CollectionAssert.Contains(names, "Addresses");
			CollectionAssert.Contains(names, SqlMigration.HistoryTableName);
		}
		finally
		{
			SqlProviderHarness.DropSqlServerDatabase(connectionString);
			Assert.IsFalse(SqlProviderHarness.IsDatabasePresent(SqlProvider.SqlServer, connectionString));
		}
	}

	[TestMethod]
	public void MigrateSampleSqliteMemory()
	{
		var connectionString = SqlProviderHarness.CreateSqliteMemoryConnectionString();
		using var database = new SampleSqlDatabase(connectionString, SqlProvider.Sqlite, null, null, null);
		database.EnsureDatabaseCreated();
		database.Migrate();
		Assert.IsTrue(database.IsDatabaseMigrated());
		var names = database.QueryTables().Select(x => x.Name).ToList();
		CollectionAssert.Contains(names, "Accounts");
		CollectionAssert.Contains(names, "Addresses");
		CollectionAssert.Contains(names, SqlMigration.HistoryTableName);
		SqliteConnection.ClearAllPools();
	}

	#endregion
}