#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Storage.Sql.Data;

public partial class SqlForeignKey : CornerstoneObject
{
	#region Properties

	public List<SqlForeignKeyColumn> Columns { get; set; }

	#endregion
}