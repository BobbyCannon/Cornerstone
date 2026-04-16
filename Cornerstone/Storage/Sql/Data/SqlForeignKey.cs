#region References

using System.Collections.Generic;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Storage.Sql.Data;

public partial class SqlForeignKey : Notifiable
{
	#region Properties

	public List<SqlForeignKeyColumn> Columns { get; set; }

	#endregion
}