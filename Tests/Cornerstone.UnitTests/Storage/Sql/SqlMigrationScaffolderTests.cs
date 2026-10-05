#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Storage.Sql.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
public class SqlMigrationScaffolderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddAndRemoveWritesFiles()
	{
		var directory = Path.Combine(Path.GetTempPath(), "SqlMigrations_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			var options = CreateOptions(directory, "Initial");
			options.PreviousSnapshot = new SchemaSnapshot();
			options.DateTimeProvider = new DateTimeProvider(() => new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc));

			var added = SqlMigrationScaffolder.Add(options);
			AreEqual("20260914120000_Initial", added.MigrationId);
			IsTrue(File.Exists(added.MigrationPath));
			IsTrue(File.Exists(added.DesignerPath));
			IsTrue(File.Exists(added.ModelSnapshotPath));
			IsTrue(File.Exists(added.RegisterPath));

			var migration = File.ReadAllText(added.MigrationPath);
			IsTrue(migration.Contains("builder.CreateTable(typeof(Cornerstone.Sample.Models.CustomerEntity));"));
			IsTrue(migration.Contains("builder.CreateTable(typeof(Cornerstone.Sample.Models.AddressEntity));"));
			IsTrue(migration.Contains("builder.CreateTable(typeof(Cornerstone.Sample.Models.AccountEntity));"));
			IsTrue(migration.Contains("builder.CreateTable(typeof(Cornerstone.Sample.Models.BookmarkEntity));"));
			IsTrue(migration.Contains("builder.DropTable(\"Accounts\");"));

			var register = File.ReadAllText(added.RegisterPath);
			IsTrue(register.Contains("new Initial(),"));

			options.Name = "AddUnused";
			options.PreviousSnapshot = SchemaSnapshot.Capture(ISampleDatabase.GetEntityTypes());
			options.DateTimeProvider = new DateTimeProvider(() => new DateTime(2026, 9, 14, 12, 1, 0, DateTimeKind.Utc));
			ExpectedException<InvalidOperationException>(() => SqlMigrationScaffolder.Add(options));

			SqlMigrationScaffolder.Remove(options);
			IsFalse(File.Exists(added.MigrationPath));
			IsFalse(File.Exists(added.DesignerPath));
			IsFalse(File.Exists(added.ModelSnapshotPath));
			var emptyRegister = File.ReadAllText(added.RegisterPath);
			IsTrue(emptyRegister.Contains("return"));
			IsFalse(emptyRegister.Contains("new Initial(),"));
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[TestMethod]
	public void AddColumnMigrationEmitsAddAndDrop()
	{
		var previous = new SchemaSnapshot();
		var previousAccounts = previous.AddTable("Accounts", string.Empty);
		previousAccounts.Columns.Add(SchemaSnapshot.CreateColumn("Id", "System.Int32", 0, false, true, true, false, -1, null));

		var current = new SchemaSnapshot();
		var currentAccounts = current.AddTable("Accounts", string.Empty);
		currentAccounts.Columns.Add(SchemaSnapshot.CreateColumn("Id", "System.Int32", 0, false, true, true, false, -1, null));
		currentAccounts.Columns.Add(SchemaSnapshot.CreateColumn("Nickname", "System.String", 99, true, false, false, false, -1, null));

		var code = SqlMigrationCodeGenerator.WriteMigration(
			"Cornerstone.Sample.Storage.Migrations",
			"AddNickname",
			"20260914120100_AddNickname",
			previous.Diff(current),
			new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
			{
				["Accounts"] = typeof(AccountEntity)
			});
		IsTrue(code.Contains("AddColumn"));
		IsTrue(code.Contains("Nickname"));
		IsTrue(code.Contains("DropColumn"));
	}

	[TestMethod]
	public void ModifyColumnMigrationEmitsRebuildTable()
	{
		var previous = SchemaSnapshot.Capture(ISampleDatabase.GetEntityTypes());
		var current = SchemaSnapshot.Capture(ISampleDatabase.GetEntityTypes());
		current.Tables.Find(x => x.Name == "Accounts").Columns.First(x => x.Name == "Name").ClrTypeName = "System.Int32";

		var code = SqlMigrationCodeGenerator.WriteMigration(
			"Cornerstone.Sample.Storage.Migrations",
			"ChangeNameType",
			"20260914120200_ChangeNameType",
			previous.Diff(current),
			new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
			{
				["Accounts"] = typeof(AccountEntity),
				["Addresses"] = typeof(AddressEntity)
			});
		IsTrue(code.Contains("builder.RebuildTable(typeof(Cornerstone.Sample.Models.AccountEntity));"));
		IsFalse(code.Contains("builder.AddColumn"));
	}

	[TestMethod]
	public void RemoveRefusesAppliedMigration()
	{
		var directory = Path.Combine(Path.GetTempPath(), "SqlMigrations_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			var options = CreateOptions(directory, "Initial");
			options.PreviousSnapshot = new SchemaSnapshot();
			options.DateTimeProvider = new DateTimeProvider(() => new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc));
			SqlMigrationScaffolder.Add(options);

			options.AppliedMigrationIds = ["20260914120000_Initial"];
			ExpectedException<InvalidOperationException>(() => SqlMigrationScaffolder.Remove(options));
			IsTrue(File.Exists(Path.Combine(directory, "20260914120000_Initial.cs")));
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[TestMethod]
	public void ToClassNameStripsSeparators()
	{
		AreEqual("AddNickname", SqlMigrationScaffolder.ToClassName("Add Nickname"));
		AreEqual("M2026Fix", SqlMigrationScaffolder.ToClassName("2026Fix"));
	}

	private static SqlMigrationScaffolderOptions CreateOptions(string directory, string name)
	{
		var options = new SqlMigrationScaffolderOptions();
		options.DatabaseType = typeof(SampleSqlDatabase);
		options.EntityTypes = ISampleDatabase.GetEntityTypes();
		options.OutputDirectory = directory;
		options.Name = name;
		options.MigrationsNamespace = "Cornerstone.Sample.Storage.Migrations";
		return options;
	}

	#endregion
}