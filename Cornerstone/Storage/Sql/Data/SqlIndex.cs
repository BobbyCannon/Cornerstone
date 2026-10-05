#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Storage.Sql.Data;

public class SqlIndex
{
	#region Constructors

	public SqlIndex()
	{
		Columns = [];
		IsClustered = false;
		IsPrimaryKey = false;
		IsUnique = false;
		Name = string.Empty;
	}

	#endregion

	#region Properties

	public List<SqlIndexColumn> Columns { get; }

	public bool IsClustered { get; set; }

	public bool IsPrimaryKey { get; set; }

	public bool IsUnique { get; set; }

	public string Name { get; set; }

	#endregion
}