#region References

using System;
using System.Linq;
using System.Management.Automation;
using Cornerstone.PowerShell.Documentation;
using Cornerstone.Storage.Sql;
using Cornerstone.Storage.Sql.Migrations;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[CmdletGroup(CmdletGroups.Sql)]
[Cmdlet(VerbsCommon.Get, "SqlMigration")]
[CmdletDescription("List registered SQL mapper migrations and whether they are applied.")]
[CmdletExample(Code = """
	Get-SqlMigration -DatabaseType SampleSqlDatabase -Assembly .\Cornerstone.Sample.dll -ConnectionString "Data Source=sample.db"
	""", Remarks = "Without ConnectionString, Applied is false for every id.")]
[CmdletsRelated(typeof(AddSqlMigrationCmdlet), typeof(RemoveSqlMigrationCmdlet), typeof(UpdateSqlDatabaseCmdlet))]
[OutputType(typeof(SqlMigrationStatus))]
public class GetSqlMigrationCmdlet : PSCmdlet
{
	#region Constructors

	public GetSqlMigrationCmdlet()
	{
		Provider = SqlProvider.Sqlite;
	}

	#endregion

	#region Properties

	[Parameter(Mandatory = false, HelpMessage = "Path to the host assembly that contains the database type.")]
	public string Assembly { get; set; }

	[Parameter(Mandatory = false, HelpMessage = "Live database used to read MigrationHistory. Optional.")]
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
			var connectionString = string.IsNullOrWhiteSpace(ConnectionString)
				? $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;"
				: ConnectionString;
			using var database = SqlMigrationHost.CreateDatabase(databaseType, connectionString, Provider);
			var applied = string.IsNullOrWhiteSpace(ConnectionString)
				? []
				: database.GetAppliedMigrationIds();
			var appliedSet = applied.ToHashSet(StringComparer.OrdinalIgnoreCase);
			foreach (var migration in database.GetRegisteredMigrations())
			{
				var status = new SqlMigrationStatus();
				status.Id = migration.Id;
				status.Applied = appliedSet.Contains(migration.Id);
				status.Pending = !status.Applied;
				WriteObject(status);
			}
		}
		catch (Exception ex)
		{
			ThrowTerminatingError(new ErrorRecord(ex, "GetSqlMigrationFailed", ErrorCategory.InvalidOperation, DatabaseType));
		}
	}

	#endregion
}
