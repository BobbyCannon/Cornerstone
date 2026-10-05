#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Storage.Sql;

[SourceReflection]
[AttributeUsage(AttributeTargets.Property)]
public class SqlIndexAttribute : Attribute
{
	#region Properties

	public bool IsUnique { get; set; }

	public string Name { get; set; }

	#endregion
}