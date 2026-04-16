#region References

using System.Data;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Storage.Sql.Data;

[Notifiable(["*"])]
public partial class SqlTableColumn : Notifiable
{
	#region Properties

	public partial string ColumnType { get; set; }
	public partial string DefaultValue { get; set; }
	public partial bool IsAutoIncrement { get; set; }
	public partial bool IsNullable { get; set; }
	public partial bool IsPrimaryKey { get; set; }
	public partial bool IsUnique { get; set; }
	public partial int MaxLength { get; set; }
	public partial string Name { get; set; }
	public partial int Order { get; set; }

	#endregion

	#region Methods

	public static SqlTableColumn FromReader(IDataRecord reader)
	{
		var response = new SqlTableColumn
		{
			Name = reader.GetString(reader.GetOrdinal("ColumnName")),
			Order = reader.GetInt32(reader.GetOrdinal("Ordinal")),
			ColumnType = reader.GetString(reader.GetOrdinal("TypeName")),

			IsNullable = reader.GetInt32(reader.GetOrdinal("IsNullable")) == 0,
			IsPrimaryKey = reader.GetInt32(reader.GetOrdinal("IsPrimaryKey")) == 1,
			IsAutoIncrement = reader.GetBoolean(reader.GetOrdinal("IsAutoIncrement")),
			IsUnique = reader.GetInt32(reader.GetOrdinal("IsUnique")) == 1,

			DefaultValue = reader.IsDBNull(reader.GetOrdinal("Default"))
				? null
				: reader.GetString(reader.GetOrdinal("Default")),

			MaxLength = reader.GetInt16(reader.GetOrdinal("MaxLength"))
		};

		return response;
	}

	#endregion
}