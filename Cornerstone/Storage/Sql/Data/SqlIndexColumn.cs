namespace Cornerstone.Storage.Sql.Data;

public class SqlIndexColumn
{
	#region Constructors

	public SqlIndexColumn()
	{
		ColumnName = string.Empty;
		IsDescending = false;
		Ordinal = 0;
	}

	#endregion

	#region Properties

	public string ColumnName { get; set; }

	public bool IsDescending { get; set; }

	public int Ordinal { get; set; }

	#endregion
}