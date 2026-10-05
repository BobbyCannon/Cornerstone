#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Storage.Sql;

[SourceReflection]
[AttributeUsage(AttributeTargets.Property)]
public class SqlForeignKeyAttribute : Attribute
{
	#region Constructors

	public SqlForeignKeyAttribute(string principalTable)
	{
		PrincipalColumn = "Id";
		PrincipalTable = principalTable;
	}

	#endregion

	#region Properties

	public string PrincipalColumn { get; set; }

	public string PrincipalTable { get; set; }

	#endregion
}