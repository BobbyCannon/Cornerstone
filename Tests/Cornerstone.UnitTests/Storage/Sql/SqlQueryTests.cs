#region References

using System;
using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Storage.Sql;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestFixture]
[NonParallelizable]
public class SqlQueryTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Query()
	{
		var connectionString = "Data Source={databaseName};Mode=Memory;Cache=Shared;";
		using var database = new SqlDatabase(connectionString, SqlProvider.Sqlite);
		var repository = database.GetRepository<Account>();
		repository.EnsureTableCreated();

		var account = new Account
		{
			Birthday = StartDateTime,
			CreatedOn = StartDateTime.AddDays(-1),
			Name = "John",
			IsDeleted = true,
			ModifiedOn = StartDateTime.AddDays(1),
			SyncId = new Guid("3B0AB396-BE53-4C48-9EFD-6C22D1098524")
		};
		repository.Add(account);

		var accounts = repository
			.Where(x => x.Name == "John")
			.Query()
			.ToArray();

		AreEqual(1, accounts.Length);
		AreEqual(StartDateTime, accounts[0].Birthday);
		AreEqual(StartDateTime.AddDays(-1), accounts[0].CreatedOn);
		AreEqual("John", accounts[0].Name);
		AreEqual(1, accounts[0].Id);
		AreEqual(true, accounts[0].IsDeleted);
		AreEqual(StartDateTime.AddDays(1), accounts[0].ModifiedOn);
		AreEqual(new Guid("3B0AB396-BE53-4C48-9EFD-6C22D1098524"), accounts[0].SyncId);
	}

	#endregion
}