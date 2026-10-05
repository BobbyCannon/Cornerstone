#region References

using System;
using System.Management.Automation;
using Cornerstone.PowerShell.Documentation;
using Cornerstone.Storage.Sql;
using Cornerstone.Storage.Sql.Migrations;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[CmdletGroup(CmdletGroups.Sql)]
[Cmdlet(VerbsCommon.Remove, "SqlMigration")]
[CmdletDescription("Remove the last unused SQL mapper migration and restore the previous snapshot.")]
[CmdletExample(Code = """
	Remove-SqlMigration -DatabaseType SampleSqlDatabase -Assembly .\Cornerstone.Sample.dll -Output .\Storage\Migrations
	""", Remarks = "Deletes the last migration if it is not in MigrationHistory.")]
[CmdletsRelated(typeof(AddSqlMigrationCmdlet), typeof(GetSqlMigrationCmdlet), typeof(UpdateSqlDatabaseCmdlet))]
[OutputType(typeof(SqlMigrationScaffoldResult))]
public class RemoveSqlMigrationCmdlet : PSCmdlet
{
	#region Constructors

	public RemoveSqlMigrationCmdlet()
	{
		Force = false;
		Provider = SqlProvider.Sqlite;
	}

	#endregion

	#region Properties

	[Parameter(Mandatory = false, HelpMessage = "Path to the host assembly that contains the database type.")]
	public string Assembly { get; set; }

	[Parameter(Mandatory = false, HelpMessage = "Live database used to refuse remove when the last migration is applied.")]
	public string ConnectionString { get; set; }

	[Parameter(Mandatory = true, Position = 0, HelpMessage = "The SqlDatabase type name (Name or FullName).")]
	public string DatabaseType { get; set; }

	[Parameter(Mandatory = false, HelpMessage = "Allow removing a migration that is already applied.")]
	public SwitchParameter Force { get; set; }

	[Parameter(Mandatory = false, HelpMessage = "Namespace for generated migration classes.")]
	public string MigrationsNamespace { get; set; }

	[Parameter(Mandatory = true, HelpMessage = "Directory for migration, snapshot, and register files.")]
	public string Output { get; set; }

	[Parameter(Mandatory = false, HelpMessage = "SQL provider for ConnectionString. Defaults to Sqlite.")]
	public SqlProvider Provider { get; set; }

	#endregion

	#region Methods

	protected override void ProcessRecord()
	{
		try
		{
			var databaseType = SqlMigrationHost.ResolveDatabaseType(Assembly, DatabaseType);
			var options = new SqlMigrationScaffolderOptions();
			options.DatabaseType = databaseType;
			options.OutputDirectory = Output;
			options.MigrationsNamespace = MigrationsNamespace;
			options.Force = Force;

			if (!string.IsNullOrWhiteSpace(ConnectionString))
			{
				using var database = SqlMigrationHost.CreateDatabase(databaseType, ConnectionString, Provider);
				options.AppliedMigrationIds = database.GetAppliedMigrationIds();
			}

			WriteObject(SqlMigrationScaffolder.Remove(options));
		}
		catch (Exception ex)
		{
			ThrowTerminatingError(new ErrorRecord(ex, "RemoveSqlMigrationFailed", ErrorCategory.InvalidOperation, DatabaseType));
		}
	}

	#endregion
}
