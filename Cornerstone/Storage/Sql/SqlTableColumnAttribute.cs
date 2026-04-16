#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Storage.Sql;

[SourceReflection]
public class SqlTableColumnAttribute : Attribute
{
	#region Properties

	public string DefaultValue { get; set; }

	public bool IsAutoIncrement { get; set; }

	public bool IsNullable { get; set; }

	public bool IsPrimaryKey { get; set; }

	public bool IsUnique { get; set; }

	public int MaxLength { get; set; }

	public string Name { get; set; }

	public int Order { get; set; }

	#endregion
}