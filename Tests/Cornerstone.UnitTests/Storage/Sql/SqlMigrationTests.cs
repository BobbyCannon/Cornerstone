#region References

using System;
using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Sample.Storage.Migrations;
using Cornerstone.Storage.Sql;
using Cornerstone.Storage.Sql.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
[DoNotParallelize]
public class SqlMigrationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddColumnEmitsProviderSqlType()
	{
		var column = SchemaSnapshot.CreateColumn("Nickname", "System.String", 0, true, false, false, false, -1, null);
		var sqlite = new MigrationBuilder(SqlProvider.Sqlite);
		sqlite.AddColumn("Accounts", column);
		AreEqual("ALTER TABLE \"Accounts\" ADD COLUMN \"Nickname\" TEXT", sqlite.Statements[0]);

		var sqlServer = new MigrationBuilder(SqlProvider.SqlServer);
		sqlServer.AddColumn("Accounts", column);
		AreEqual("ALTER TABLE [Accounts] ADD [Nickname] NVARCHAR(MAX)", sqlServer.Statements[0]);
	}

	[TestMethod]
	public void AddColumnNotNullIncludesDefault()
	{
		var column = SchemaSnapshot.CreateColumn("Age", "System.Int32", 0, false, false, false, false, -1, null);
		var sqlite = new MigrationBuilder(SqlProvider.Sqlite);
		sqlite.AddColumn("Accounts", column);
		AreEqual("ALTER TABLE \"Accounts\" ADD COLUMN \"Age\" INTEGER NOT NULL DEFAULT 0", sqlite.Statements[0]);
	}

	[TestMethod]
	public void CreateTableUsesGeneratedScript()
	{
		var builder = new MigrationBuilder(SqlProvider.Sqlite);
		builder.CreateTable(typeof(AccountEntity));
		IsTrue(builder.Statements[0].Contains("CREATE TABLE \"Accounts\""));
		IsFalse(builder.Statements[0].Contains("IF NOT EXISTS"));
		IsTrue(builder.Statements[0].Contains("\"EmailAddress\""));
	}

	[TestMethod]
	public void DatabaseWithoutMigrationsIsNotMigrated()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		using var database = new SqlDatabase(connectionString, SqlProvider.Sqlite);
		IsFalse(database.IsDatabaseMigrated());
	}

	[TestMethod]
	public void IsDatabaseMigratedFollowsHistoryAcrossInstances()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		using var first = new SampleSqlDatabase(connectionString, SqlProvider.Sqlite, null, null, this);
		IsFalse(first.IsDatabaseMigrated());
		first.Migrate();
		IsTrue(first.IsDatabaseMigrated());

		using var second = new SampleSqlDatabase(connectionString, SqlProvider.Sqlite, null, null, this);
		IsTrue(second.IsDatabaseMigrated());
		AreEqual(0, second.GetPendingMigrations().Count);
	}

	[TestMethod]
	public void MigrateAddsMissingColumnsOnExistingTable()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		using var database = new SampleSqlDatabase(connectionString, SqlProvider.Sqlite, null, null, this);
		using (var connection = database.CreateConnection())
		{
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandText =
				"""
				CREATE TABLE "Accounts"
				(
					"Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					"Name" TEXT
				)
				""";
			command.ExecuteNonQuery();
		}

		database.Migrate();

		var accounts = database.QueryTables().First(x => x.Name == "Accounts");
		var names = accounts.Columns.Select(x => x.Name).ToList();
		IsTrue(names.Contains("Id"));
		IsTrue(names.Contains("Name"));
		IsTrue(names.Contains("AddressId"));
		IsTrue(names.Contains("EmailAddress"));
		IsTrue(names.Contains("SyncId"));
		IsTrue(database.QueryTables().Any(x => x.Name == "Addresses"));
	}

	[TestMethod]
	public void MigrateAppliesAddColumn()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		var migrations = new SqlMigration[] { new Initial(), new AddNicknameMigration() };
		using var database = new SqlDatabase(connectionString, SqlProvider.Sqlite, migrations);
		database.Migrate();

		var accounts = database.QueryTables().First(x => x.Name == "Accounts");
		IsTrue(accounts.Columns.Any(x => x.Name == "Nickname"));
		AreEqual(2, database.GetAppliedMigrationIds().Count);
	}

	[TestMethod]
	public void MigrateAppliesInitialAndIsIdempotent()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		using var database = new SampleSqlDatabase(connectionString, SqlProvider.Sqlite, null, null, this);

		AreEqual(1, database.GetPendingMigrations().Count);
		database.Migrate();

		var applied = database.GetAppliedMigrationIds();
		AreEqual(1, applied.Count);
		AreEqual("20260914120000_Initial", applied[0]);
		AreEqual(0, database.GetPendingMigrations().Count);

		var names = database.QueryTables().Select(x => x.Name).ToList();
		IsTrue(names.Contains("Accounts"));
		IsTrue(names.Contains("Addresses"));
		IsTrue(names.Contains("Bookmarks"));
		IsTrue(names.Contains("Customers"));
		IsTrue(names.Contains("Settings"));
		IsTrue(names.Contains(SqlMigration.HistoryTableName));

		var accounts = database.QueryTables().First(x => x.Name == "Accounts");
		IsTrue(accounts.Indexes.Any(x => x.Columns.Any(c => c.ColumnName == "LastLoginDate")));
		IsTrue(accounts.Indexes.Any(x => x.Columns.Any(c => c.ColumnName == "ModifiedOn")));
		IsTrue(accounts.ForeignKeys.Any(x => x.PrincipalTable == "Addresses"));
		IsTrue(database.QueryTables().First(x => x.Name == "Addresses").Indexes.Any(x => x.Columns.Any(c => c.ColumnName == "ModifiedOn")));
		IsTrue(database.QueryTables().First(x => x.Name == "Bookmarks").Indexes.Any(x => x.Columns.Any(c => c.ColumnName == "ModifiedOn")));
		IsTrue(database.QueryTables().First(x => x.Name == "Customers").Indexes.Any(x => x.Columns.Any(c => c.ColumnName == "ModifiedOn")));
		IsTrue(database.QueryTables().First(x => x.Name == "Settings").Indexes.Any(x => x.Columns.Any(c => c.ColumnName == "ModifiedOn")));

		database.Migrate();
		AreEqual(1, database.GetAppliedMigrationIds().Count);
		AreEqual(0, database.Accounts.ReadAllKeys().Count);
	}

	[TestMethod]
	public void MigrateRebuildsSqliteWhenDeclaredTypeChanges()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		using var database = new SampleSqlDatabase(connectionString, SqlProvider.Sqlite, null, null, this);
		using (var connection = database.CreateConnection())
		{
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandText =
				"""
				CREATE TABLE "Accounts"
				(
					"Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					"Name" TEXT,
					"CreatedOn" TEXT NOT NULL
				)
				""";
			command.ExecuteNonQuery();
			command.CommandText = """INSERT INTO "Accounts" ("Name", "CreatedOn") VALUES ('Ann', '2020-01-02')""";
			command.ExecuteNonQuery();
		}

		database.Migrate();

		var createdOn = database.QueryTables().First(x => x.Name == "Accounts").Columns.First(x => x.Name == "CreatedOn");
		AreEqual("DATE", SqlGenerator.NormalizeDeclaredType(createdOn.ColumnType));

		var rows = database.GetRepository<AccountEntity>().Where(x => x.Name == "Ann").Query().ToArray();
		AreEqual(1, rows.Length);
		AreEqual("Ann", rows[0].Name);
	}

	#endregion

	#region Classes

	private sealed class AddNicknameMigration : SqlMigration
	{
		#region Properties

		public override string Id => "20260914120100_AddNickname";

		#endregion

		#region Methods

		public override void Up(MigrationBuilder builder)
		{
			builder.AddColumn("Accounts", SchemaSnapshot.CreateColumn("Nickname", "System.String", 0, true, false, false, false, -1, null));
		}

		#endregion
	}

	#endregion
}