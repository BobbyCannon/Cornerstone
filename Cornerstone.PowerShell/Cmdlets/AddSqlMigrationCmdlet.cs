#region References

using System;
using System.Management.Automation;
using Cornerstone.PowerShell.Documentation;
using Cornerstone.Storage.Sql.Migrations;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[CmdletGroup(CmdletGroups.Sql)]
[Cmdlet(VerbsCommon.Add, "SqlMigration")]
[CmdletDescription("Add a SQL mapper migration by diffing the current model against the last snapshot.")]
[CmdletExample(Code = """
	Add-SqlMigration -Name AddNickname -DatabaseType SampleSqlDatabase -Assembly .\Cornerstone.Sample.dll -Output .\Storage\Migrations
	""", Remarks = "Scaffolds a migration after changing entity columns.")]
[CmdletsRelated(typeof(RemoveSqlMigrationCmdlet), typeof(GetSqlMigrationCmdlet), typeof(UpdateSqlDatabaseCmdlet))]
[OutputType(typeof(SqlMigrationScaffoldResult))]
public class AddSqlMigrationCmdlet : PSCmdlet
{
	#region Properties

	[Parameter(Mandatory = false, HelpMessage = "Path to the host assembly that contains the database type.")]
	public string Assembly { get; set; }

	[Parameter(Mandatory = true, Position = 1, HelpMessage = "The SqlDatabase type name (Name or FullName).")]
	public string DatabaseType { get; set; }

	[Parameter(Mandatory = false, HelpMessage = "Namespace for generated migration classes.")]
	public string MigrationsNamespace { get; set; }

	[Parameter(Mandatory = true, Position = 0, HelpMessage = "The migration name, for example AddNickname.")]
	public string Name { get; set; }

	[Parameter(Mandatory = true, HelpMessage = "Directory for migration, snapshot, and register files.")]
	public string Output { get; set; }

	#endregion

	#region Methods

	protected override void ProcessRecord()
	{
		try
		{
			var databaseType = SqlMigrationHost.ResolveDatabaseType(Assembly, DatabaseType);
			var options = new SqlMigrationScaffolderOptions();
			options.DatabaseType = databaseType;
			options.EntityTypes = SqlMigrationHost.ResolveEntityTypes(databaseType);
			options.OutputDirectory = Output;
			options.Name = Name;
			options.MigrationsNamespace = MigrationsNamespace;
			WriteObject(SqlMigrationScaffolder.Add(options));
		}
		catch (Exception ex)
		{
			ThrowTerminatingError(new ErrorRecord(ex, "AddSqlMigrationFailed", ErrorCategory.InvalidOperation, DatabaseType));
		}
	}

	#endregion
}
