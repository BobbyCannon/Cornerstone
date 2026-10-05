namespace Cornerstone.Storage.Sql.Data;

public class SqlForeignKeyColumn
{
	#region Constructors

	public SqlForeignKeyColumn()
	{
		ColumnName = string.Empty;
		Ordinal = 0;
	}

	#endregion

	#region Properties

	public string ColumnName { get; set; }

	public int Ordinal { get; set; }

	#endregion
}