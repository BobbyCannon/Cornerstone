namespace Cornerstone.Storage.Sql.Data;

public class SqlIndexColumn
{
	#region Properties

	public string ColumnName { get; set; }

	public bool IsDescending { get; set; }

	public int Ordinal { get; set; }

	#endregion
}