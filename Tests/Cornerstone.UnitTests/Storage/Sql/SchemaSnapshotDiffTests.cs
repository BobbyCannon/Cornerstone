#region References

using System.Linq;
using Cornerstone.Sample.Storage;
using Cornerstone.Storage.Sql.Data;
using Cornerstone.Storage.Sql.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
public class SchemaSnapshotDiffTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddColumn()
	{
		var previous = Snapshot("Accounts", SchemaSnapshot.CreateColumn("Id", "System.Int32", 0, false, true, true, false, -1, null));
		var current = Snapshot("Accounts",
			SchemaSnapshot.CreateColumn("Id", "System.Int32", 0, false, true, true, false, -1, null),
			SchemaSnapshot.CreateColumn("Name", "System.String", 1, false, false, false, false, -1, null));

		var diff = previous.Diff(current);
		AreEqual(1, diff.Tables.Count);
		AreEqual(TableAction.Alter, diff.Tables[0].Action);
		AreEqual(1, diff.Tables[0].Columns.Count);
		AreEqual(ColumnAction.Add, diff.Tables[0].Columns[0].Action);
		AreEqual("Name", diff.Tables[0].Columns[0].Name);
		AreEqual("System.String", diff.Tables[0].Columns[0].Column.ClrTypeName);
	}

	[TestMethod]
	public void CreateAndDropTables()
	{
		var previous = Snapshot("Accounts", SchemaSnapshot.CreateColumn("Id", "System.Int32", 0, false, true, true, false, -1, null));
		var current = Snapshot("Addresses", SchemaSnapshot.CreateColumn("Id", "System.Int64", 0, false, true, true, false, -1, null));

		var diff = previous.Diff(current);
		AreEqual(2, diff.Tables.Count);
		AreEqual(TableAction.Drop, diff.Tables.First(x => x.TableName == "Accounts").Action);
		AreEqual(TableAction.Create, diff.Tables.First(x => x.TableName == "Addresses").Action);
		IsTrue(diff.Tables.First(x => x.TableName == "Addresses").Columns.Count == 0);
	}

	[TestMethod]
	public void DropColumn()
	{
		var previous = Snapshot("Accounts",
			SchemaSnapshot.CreateColumn("Id", "System.Int32", 0, false, true, true, false, -1, null),
			SchemaSnapshot.CreateColumn("Name", "System.String", 1, false, false, false, false, -1, null));
		var current = Snapshot("Accounts", SchemaSnapshot.CreateColumn("Id", "System.Int32", 0, false, true, true, false, -1, null));

		var diff = previous.Diff(current);
		AreEqual(ColumnAction.Drop, diff.Tables[0].Columns[0].Action);
		AreEqual("Name", diff.Tables[0].Columns[0].Name);
	}

	[TestMethod]
	public void EmptyPreviousCreatesMappedTables()
	{
		var diff = new SchemaSnapshot().Diff(SchemaSnapshot.Capture(ISampleDatabase.GetEntityTypes()));
		IsTrue(diff.HasChanges);
		AreEqual("Accounts, Addresses, Bookmarks, Customers, Settings", string.Join(", ", diff.Tables.Select(x => x.TableName)));
		IsTrue(diff.Tables.All(x => x.Action == TableAction.Create));
	}

	[TestMethod]
	public void IdenticalSnapshotsHaveNoChanges()
	{
		var previous = SchemaSnapshot.Capture(ISampleDatabase.GetEntityTypes());
		var current = SchemaSnapshot.Capture(ISampleDatabase.GetEntityTypes());
		var diff = previous.Diff(current);
		IsFalse(diff.HasChanges);
	}

	[TestMethod]
	public void ModifyColumnTypeAndNullability()
	{
		var previous = Snapshot("Accounts", SchemaSnapshot.CreateColumn("Age", "System.Int32", 0, false, false, false, false, -1, null));
		var current = Snapshot("Accounts", SchemaSnapshot.CreateColumn("Age", "System.Int64", 0, true, false, false, false, -1, null));

		var change = previous.Diff(current).Tables[0].Columns[0];
		AreEqual(ColumnAction.Modify, change.Action);
		AreEqual("System.Int32", change.Previous.ClrTypeName);
		AreEqual("System.Int64", change.Column.ClrTypeName);
		IsFalse(change.Previous.IsNullable);
		IsTrue(change.Column.IsNullable);
	}

	[TestMethod]
	public void OrderOnlyChangeIsIgnored()
	{
		var previous = Snapshot("Accounts",
			SchemaSnapshot.CreateColumn("Id", "System.Int32", 0, false, true, true, false, -1, null),
			SchemaSnapshot.CreateColumn("Name", "System.String", 1, false, false, false, false, -1, null));
		var current = Snapshot("Accounts",
			SchemaSnapshot.CreateColumn("Id", "System.Int32", 5, false, true, true, false, -1, null),
			SchemaSnapshot.CreateColumn("Name", "System.String", 0, false, false, false, false, -1, null));

		IsFalse(previous.Diff(current).HasChanges);
	}

	private static SchemaSnapshot Snapshot(string tableName, params SqlTableColumn[] columns)
	{
		var snapshot = new SchemaSnapshot();
		var table = snapshot.AddTable(tableName, string.Empty);
		foreach (var column in columns)
		{
			table.Columns.Add(column);
		}

		return snapshot;
	}

	#endregion
}