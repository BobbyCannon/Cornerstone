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
	public void QueryTables()
	{
		ForEachProvider(database =>
		{
			database.EnsureDatabaseCreated();

			var repository = database.GetRepository<AccountEntity>();
			repository.EnsureTableCreated();

			var actual = database.QueryTables().First(x => x.Name == "Accounts");
			IsNotNull(actual);
			AreEqual(
				"CreatedOn, EmailAddress, Id, IsDeleted, LastLoginDate, ModifiedOn, Name, Picture, Roles, Status, SyncId, TimeZoneId",
				string.Join(", ", actual.Columns.Select(x => x.Name).ToArray())
			);
		});
	}

	protected void ForEachProvider(Action<SqlDatabase> process)
	{
		var connectionString = "Data Source={databaseName};Mode=Memory;Cache=Shared;";
		using (var database = new SqlDatabase(connectionString, SqlProvider.Sqlite))
		{
			//database.Provider.Dump();
			process.Invoke(database);
		}

		//var connectionString = "server=bobbys-rig;database=Sample;integrated security=true;encrypt=false;";
		//using (var database = new SqlDatabase(connectionString, SqlProvider.SqlServer))
		//{
		//	//database.Provider.Dump();
		//	process.Invoke(database);
		//}
	}

	#endregion
}