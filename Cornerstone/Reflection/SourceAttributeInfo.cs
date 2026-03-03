#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public class SourceAttributeInfo
{
	#region Properties

	public object[] ConstructorArguments { get; init; }

	public string FullyGlobalQualifiedName { get; init; }

	public string FullyQualifiedName { get; init; }

	public string Name { get; init; }

	public IDictionary<string, object> NamedArguments { get; init; }

	public Type Type { get; init; }

	#endregion
}