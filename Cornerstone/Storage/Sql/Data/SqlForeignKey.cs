#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Storage.Sql.Data;

public partial class SqlForeignKey : CornerstoneObject
{
	#region Constructors

	public SqlForeignKey()
	{
		Columns = [];
		Name = string.Empty;
		PrincipalColumn = "Id";
		PrincipalTable = string.Empty;
	}

	#endregion

	#region Properties

	public List<SqlForeignKeyColumn> Columns { get; }

	public string Name { get; set; }

	public string PrincipalColumn { get; set; }

	public string PrincipalTable { get; set; }

	#endregion
}