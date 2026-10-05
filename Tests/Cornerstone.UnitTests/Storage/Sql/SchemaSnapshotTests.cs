#region References

using System.Linq;
using Cornerstone.Sample.Storage;
using Cornerstone.Storage.Sql.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
public class SchemaSnapshotTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CaptureIncludesMappedTablesAndColumns()
	{
		var snapshot = SchemaSnapshot.Capture(ISampleDatabase.GetEntityTypes());

		AreEqual("Accounts, Addresses, Bookmarks, Customers, Settings", string.Join(", ", snapshot.Tables.Select(x => x.Name)));

		var accounts = snapshot.Tables.First(x => x.Name == "Accounts");
		var names = string.Join(", ", accounts.Columns.Select(x => x.Name));
		IsTrue(names.Contains("Id"));
		IsTrue(names.Contains("SyncId"));
		IsTrue(names.Contains("EmailAddress"));
		IsTrue(names.Contains("CreatedOn"));

		var id = accounts.Columns.First(x => x.Name == "Id");
		AreEqual("System.Int32", id.ClrTypeName);
		IsTrue(id.IsPrimaryKey);
		IsTrue(id.IsAutoIncrement);

		var syncId = accounts.Columns.First(x => x.Name == "SyncId");
		AreEqual("System.Guid", syncId.ClrTypeName);
		IsTrue(syncId.IsUnique);

		var email = accounts.Columns.First(x => x.Name == "EmailAddress");
		AreEqual("System.String", email.ClrTypeName);
		IsFalse(email.IsNullable);

		IsTrue(accounts.Indexes.Any(x => x.IsUnique && x.Columns.Any(c => c.ColumnName == "SyncId")));
		IsTrue(accounts.Indexes.Any(x => !x.IsUnique && x.Columns.Any(c => c.ColumnName == "LastLoginDate")));
		IsTrue(accounts.Indexes.Any(x => !x.IsUnique && x.Columns.Any(c => c.ColumnName == "ModifiedOn")));
		IsTrue(accounts.ForeignKeys.Any(x => (x.PrincipalTable == "Addresses") && x.Columns.Any(c => c.ColumnName == "AddressId")));
	}

	[TestMethod]
	public void ToCSharpIsDeterministicAndRebuilds()
	{
		var snapshot = SchemaSnapshot.Capture(ISampleDatabase.GetEntityTypes());
		var csharp = snapshot.ToCSharp("Cornerstone.Sample.Storage.Migrations", "SampleSqlDatabaseModelSnapshot");

		IsTrue(csharp.Contains("namespace Cornerstone.Sample.Storage.Migrations;"));
		IsTrue(csharp.Contains("public partial class SampleSqlDatabaseModelSnapshot : SchemaSnapshot"));
		IsTrue(csharp.Contains("AddTable(\"Accounts\", \"\")"));
		IsTrue(csharp.Contains("AddTable(\"Addresses\", \"\")"));
		IsTrue(csharp.Contains("AddTable(\"Bookmarks\", \"\")"));
		IsTrue(csharp.Contains("AddTable(\"Customers\", \"\")"));
		IsTrue(csharp.Contains("AddTable(\"Settings\", \"\")"));
		IsTrue(csharp.Contains("\"System.Int32\""));
		IsTrue(csharp.Contains("\"System.Guid\""));

		var rebuilt = SchemaSnapshot.Capture(ISampleDatabase.GetEntityTypes());
		AreEqual(csharp, rebuilt.ToCSharp("Cornerstone.Sample.Storage.Migrations", "SampleSqlDatabaseModelSnapshot"));

		var reconstructed = new SchemaSnapshot();
		foreach (var table in snapshot.Tables)
		{
			var added = reconstructed.AddTable(table.Name, table.Schema);
			foreach (var column in table.Columns)
			{
				added.Columns.Add(SchemaSnapshot.CreateColumn(
					column.Name,
					column.ClrTypeName,
					column.Order,
					column.IsNullable,
					column.IsPrimaryKey,
					column.IsAutoIncrement,
					column.IsUnique,
					column.MaxLength,
					column.DefaultValue));
			}

			foreach (var index in table.Indexes)
			{
				var columnName = index.Columns.Count > 0 ? index.Columns[0].ColumnName : string.Empty;
				added.Indexes.Add(SchemaSnapshot.CreateIndex(index.Name, index.IsUnique, columnName));
			}

			foreach (var fk in table.ForeignKeys)
			{
				var columnName = fk.Columns.Count > 0 ? fk.Columns[0].ColumnName : string.Empty;
				added.ForeignKeys.Add(SchemaSnapshot.CreateForeignKey(fk.Name, fk.PrincipalTable, fk.PrincipalColumn, columnName));
			}
		}

		AreEqual(snapshot.ToCSharp("N", "S"), reconstructed.ToCSharp("N", "S"));
	}

	#endregion
}