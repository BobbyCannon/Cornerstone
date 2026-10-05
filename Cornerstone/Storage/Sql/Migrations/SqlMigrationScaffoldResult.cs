namespace Cornerstone.Storage.Sql.Migrations;

public class SqlMigrationScaffoldResult
{
	#region Constructors

	public SqlMigrationScaffoldResult()
	{
		ClassName = string.Empty;
		DesignerPath = string.Empty;
		MigrationId = string.Empty;
		MigrationPath = string.Empty;
		ModelSnapshotPath = string.Empty;
		RegisterPath = string.Empty;
	}

	#endregion

	#region Properties

	public string ClassName { get; set; }

	public string DesignerPath { get; set; }

	public string MigrationId { get; set; }

	public string MigrationPath { get; set; }

	public string ModelSnapshotPath { get; set; }

	public string RegisterPath { get; set; }

	#endregion
}