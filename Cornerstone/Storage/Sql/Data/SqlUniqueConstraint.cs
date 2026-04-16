#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Storage.Sql.Data;

public class SqlUniqueConstraint
{
	#region Constructors

	public SqlUniqueConstraint()
	{
		Columns = [];
	}

	#endregion

	#region Properties

	public List<SqlTableColumn> Columns { get; }

	public string Name { get; set; }

	#endregion
}