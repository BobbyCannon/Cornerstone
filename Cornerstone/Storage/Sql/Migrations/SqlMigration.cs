namespace Cornerstone.Storage.Sql.Migrations;

/// <summary>
/// One versioned schema change. Id is the history key (timestamp plus name).
/// </summary>
public abstract class SqlMigration
{
	#region Constants

	public const string HistoryTableName = "MigrationHistory";

	#endregion

	#region Properties

	public abstract string Id { get; }

	#endregion

	#region Methods

	public virtual void Down(MigrationBuilder builder)
	{
	}

	public virtual void Up(MigrationBuilder builder)
	{
	}

	#endregion
}