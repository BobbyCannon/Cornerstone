#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Storage.Sql.Data;

public class SqlIndex
{
	#region Properties

	public List<SqlIndexColumn> Columns { get; set; }

	public bool IsClustered { get; set; }

	public bool IsPrimaryKey { get; set; }

	public bool IsUnique { get; set; }

	public string Name { get; set; }

	#endregion
}