#region References

using System;
using System.IO;
using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Storage.Sql;
using Cornerstone.Storage.Sql.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
[DoNotParallelize]
public class SqlMigrationHostTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CreateDatabaseMigrateAppliesInitial()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		using var database = SqlMigrationHost.CreateDatabase(typeof(SampleSqlDatabase), connectionString, SqlProvider.Sqlite);
		AreEqual(1, database.GetRegisteredMigrations().Count);
		AreEqual("20260914120000_Initial", database.GetRegisteredMigrations().Last().Id);
		AreEqual(1, database.GetPendingMigrations().Count);
		database.Migrate();
		AreEqual(0, database.GetPendingMigrations().Count);
		AreEqual(1, database.GetAppliedMigrationIds().Count);
		AreEqual("20260914120000_Initial", database.GetAppliedMigrationIds().Last());
	}

	[TestMethod]
	public void ResolveDatabaseTypeByName()
	{
		var type = SqlMigrationHost.ResolveDatabaseType(null, nameof(SampleSqlDatabase));
		AreEqual(typeof(SampleSqlDatabase), type);
	}

	[TestMethod]
	public void ResolveEntityTypesFromInterface()
	{
		var types = SqlMigrationHost.ResolveEntityTypes(typeof(SampleSqlDatabase));
		IsTrue(types.Contains(typeof(AccountEntity)));
		IsTrue(types.Contains(typeof(AddressEntity)));
	}

	[TestMethod]
	public void UpdateSqlDatabaseAgainstSampleAppliesPendingThenIsIdempotent()
	{
		var databaseFile = Path.Combine(Path.GetTempPath(), "SampleUpdate_" + Guid.NewGuid().ToString("N") + ".db");
		var connectionString = "Data Source=" + databaseFile;
		try
		{
			var databaseType = SqlMigrationHost.ResolveDatabaseType(null, nameof(SampleSqlDatabase));
			AreEqual(typeof(SampleSqlDatabase), databaseType);

			string[] applied;
			using (var database = SqlMigrationHost.CreateDatabase(databaseType, connectionString, SqlProvider.Sqlite))
			{
				var pending = database.GetPendingMigrations();
				AreEqual(1, pending.Count);
				AreEqual("20260914120000_Initial", pending[0].Id);
				database.Migrate();
				applied = pending.Select(x => x.Id).ToArray();
				AreEqual(0, database.GetPendingMigrations().Count);
				IsTrue(database.IsDatabaseMigrated());
				var accounts = database.QueryTables().First(x => x.Name == "Accounts");
				IsTrue(accounts.Indexes.Any(x => x.Columns.Any(c => c.ColumnName == "LastLoginDate")));
				IsTrue(accounts.ForeignKeys.Any(x => x.PrincipalTable == "Addresses"));
			}

			AreEqual(1, applied.Length);

			using (var database = SqlMigrationHost.CreateDatabase(databaseType, connectionString, SqlProvider.Sqlite))
			{
				AreEqual(0, database.GetPendingMigrations().Count);
				IsTrue(database.IsDatabaseMigrated());
			}
		}
		finally
		{
			SqliteConnection.ClearAllPools();
			if (File.Exists(databaseFile))
			{
				File.Delete(databaseFile);
			}
		}
	}

	#endregion
}