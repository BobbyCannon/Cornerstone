#region References

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceAttributeInfo
{
	#region Fields

	public object[] ConstructorArguments;
	public AttributeData Data;
	public string FullyGlobalQualifiedName;
	public string FullyQualifiedName;
	public string Name;
	public IDictionary<string, object> NamedArguments;
	public Type Type;
	public INamedTypeSymbol TypeSymbol;

	#endregion
}