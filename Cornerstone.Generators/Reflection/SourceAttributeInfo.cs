#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public partial class SourceAttributeInfo
{
	#region Properties

	public object[] ConstructorArguments { get; set; }
	public string FullyGlobalQualifiedName { get; set; }
	public string FullyQualifiedName { get; set; }
	public string Name { get; set; }
	public IDictionary<string, object> NamedArguments { get; set; }
	public Type Type { get; set; }

	#endregion
}