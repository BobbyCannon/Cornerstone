#region References

using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Storage.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Storage;

[TestClass]
[DoNotParallelize]
public class SqlDatabaseQueryTablesTests
{
	#region Constants

	private const string AccountColumns = "AddressId, AddressSyncId, CreatedOn, CustomerId, CustomerSyncId, EmailAddress, Id, IsDeleted, LastLoginDate, ModifiedOn, Name, Picture, Roles, Status, SyncId, TimeZoneId";

	#endregion

	#region Methods

	[TestMethod]
	public void QueryTablesSqlServerAfterEnsure()
	{
		var connectionString = SqlProviderHarness.SqlServerConnectionString();
		SqlProviderHarness.RequireSqlServer(connectionString);
		SqlProviderHarness.DropSqlServerDatabase(connectionString);

		try
		{
			using var database = new SqlDatabase(connectionString, SqlProvider.SqlServer);
			database.EnsureDatabaseCreated();
			database.GetRepository<AddressEntity>().EnsureTableCreated();
			database.GetRepository<AccountEntity>().EnsureTableCreated();

			var actual = database.QueryTables().First(x => x.Name == "Accounts");
			Assert.IsNotNull(actual);
			Assert.AreEqual(AccountColumns, string.Join(", ", actual.Columns.Select(x => x.Name)));
		}
		finally
		{
			SqlProviderHarness.DropSqlServerDatabase(connectionString);
		}
	}

	[TestMethod]
	public void QueryTablesSqlServerAfterMigrate()
	{
		var connectionString = SqlProviderHarness.SqlServerConnectionString();
		SqlProviderHarness.RequireSqlServer(connectionString);
		SqlProviderHarness.DropSqlServerDatabase(connectionString);

		try
		{
			using var database = new SampleSqlDatabase(connectionString, SqlProvider.SqlServer, null, null, null);
			database.EnsureDatabaseCreated();
			database.Migrate();

			var actual = database.QueryTables().First(x => x.Name == "Accounts");
			Assert.IsNotNull(actual);
			Assert.AreEqual(AccountColumns, string.Join(", ", actual.Columns.Select(x => x.Name)));
			Assert.AreEqual(actual.Columns.Count, actual.Columns.Select(x => x.Name).Distinct().Count());
			Assert.IsTrue(actual.ForeignKeys.Any(x => x.PrincipalTable == "Addresses"));
		}
		finally
		{
			SqlProviderHarness.DropSqlServerDatabase(connectionString);
		}
	}

	#endregion
}
