#region References

using System;
using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Storage.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
[DoNotParallelize]
public class SqlDatabaseTests : GeneratorUnitTest
{
	#region Methods

	[TestMethod]
	public void QueryPrivateMemorySqliteSeesCreatedTables()
	{
		// Mode=Memory without Cache=Shared is connection-private. Queries must
		// reuse the keep-alive connection or they see a different empty database.
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;";
		using var database = new SqlDatabase(connectionString, SqlProvider.Sqlite);
		database.GetRepository<AddressEntity>().EnsureTableCreated();
		var repository = database.GetRepository<AccountEntity>();
		repository.EnsureTableCreated();
		repository.Add(new AccountEntity
		{
			CreatedOn = StartDateTime,
			EmailAddress = "john@domain.com",
			ModifiedOn = StartDateTime,
			Name = "John",
			Roles = ",,",
			SyncId = Guid.NewGuid()
		});
		AreEqual(0, repository.Count());
		database.SaveChanges();
		AreEqual(1, repository.Count());
		AreEqual("John", repository.Where(x => x.Name == "John").Query().Single().Name);
	}

	[TestMethod]
	public void QueryTablesSqliteMemory()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		using var database = new SqlDatabase(connectionString, SqlProvider.Sqlite);
		database.GetRepository<AddressEntity>().EnsureTableCreated();
		database.GetRepository<AccountEntity>().EnsureTableCreated();

		var actual = database.QueryTables().First(x => x.Name == "Accounts");
		IsNotNull(actual);
		AreEqual(
			"AddressId, AddressSyncId, CreatedOn, CustomerId, CustomerSyncId, EmailAddress, Id, IsDeleted, LastLoginDate, ModifiedOn, Name, Picture, Roles, Status, SyncId, TimeZoneId",
			string.Join(", ", actual.Columns.Select(x => x.Name).ToArray())
		);
	}

	#endregion
}