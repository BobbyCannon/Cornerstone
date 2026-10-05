#region References

using System;
using System.Management.Automation;
using Cornerstone.PowerShell.Documentation;
using Cornerstone.Storage.Sql;
using Cornerstone.Storage.Sql.Migrations;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[CmdletGroup(CmdletGroups.Sql)]
[Cmdlet(VerbsData.Update, "SqlDatabase")]
[CmdletDescription("Apply pending SQL mapper migrations to a database.")]
[CmdletExample(Code = """
	Update-SqlDatabase -DatabaseType SampleSqlDatabase -Assembly .\Cornerstone.Sample.dll -ConnectionString "Data Source=sample.db"
	""", Remarks = "Runs Migrate() and writes the ids that were pending.")]
[CmdletsRelated(typeof(AddSqlMigrationCmdlet), typeof(RemoveSqlMigrationCmdlet), typeof(GetSqlMigrationCmdlet))]
[OutputType(typeof(string))]
public class UpdateSqlDatabaseCmdlet : PSCmdlet
{
	#region Constructors

	public UpdateSqlDatabaseCmdlet()
	{
		Provider = SqlProvider.Sqlite;
	}

	#endregion

	#region Properties

	[Parameter(Mandatory = false, HelpMessage = "Path to the host assembly that contains the database type.")]
	public string Assembly { get; set; }

	[Parameter(Mandatory = true, HelpMessage = "Connection string for the database to update.")]
	public string ConnectionString { get; set; }

	[Parameter(Mandatory = true, Position = 0, HelpMessage = "The SqlDatabase type name (Name or FullName).")]
	public string DatabaseType { get; set; }

	[Parameter(Mandatory = false, HelpMessage = "SQL provider. Defaults to Sqlite.")]
	public SqlProvider Provider { get; set; }

	#endregion

	#region Methods

	protected override void ProcessRecord()
	{
		try
		{
			if (Provider == SqlProvider.Sqlite)
			{
				SqliteNative.Init();
			}

			var databaseType = SqlMigrationHost.ResolveDatabaseType(Assembly, DatabaseType);
			using var database = SqlMigrationHost.CreateDatabase(databaseType, ConnectionString, Provider);
			var pending = database.GetPendingMigrations();
			database.Migrate();
			foreach (var migration in pending)
			{
				WriteObject(migration.Id);
			}
		}
		catch (Exception ex)
		{
			ThrowTerminatingError(new ErrorRecord(ex, "UpdateSqlDatabaseFailed", ErrorCategory.InvalidOperation, DatabaseType));
		}
	}

	#endregion
}
