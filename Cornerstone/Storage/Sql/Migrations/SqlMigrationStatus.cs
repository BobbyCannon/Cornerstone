namespace Cornerstone.Storage.Sql.Migrations;

/// <summary>
/// One registered migration and whether it is applied.
/// </summary>
public class SqlMigrationStatus
{
	#region Constructors

	public SqlMigrationStatus()
	{
		Id = string.Empty;
	}

	#endregion

	#region Properties

	public bool Applied { get; set; }

	public string Id { get; set; }

	public bool Pending { get; set; }

	#endregion
}