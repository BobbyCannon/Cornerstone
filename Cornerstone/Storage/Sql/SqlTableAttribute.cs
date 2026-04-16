#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Storage.Sql;

[SourceReflection]
public class SqlTableAttribute : Attribute
{
	#region Properties

	public string TableName { get; set; }

	#endregion
}