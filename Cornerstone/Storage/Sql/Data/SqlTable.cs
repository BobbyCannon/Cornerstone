#region References

using System.Collections.Generic;
using System.Data;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Storage.Sql.Data;

[Notifiable(["*"])]
public partial class SqlTable : Notifiable
{
	#region Constructors

	public SqlTable(IDataRecord reader)
		: this(reader["TableName"].ToString(),
			reader["SchemaName"].ToString())
	{
	}

	public SqlTable(string name, string schema)
	{
		Name = name;
		Schema = schema;
		Columns = [];
		ForeignKeys = [];
	}

	#endregion

	#region Properties

	public List<SqlTableColumn> Columns { get; }

	public List<SqlForeignKey> ForeignKeys { get; }

	public partial string Name { get; set; }

	public partial string Schema { get; set; }

	#endregion
}