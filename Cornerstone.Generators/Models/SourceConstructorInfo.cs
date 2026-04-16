#region References

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceConstructorInfo : Reflection.SourceConstructorInfo
{
	#region Properties

	public new Accessibility Accessibility { get; set; }
	public new List<SourceAttributeInfo> Attributes { get; } = [];
	public INamedTypeSymbol ContainingType { get; set; }
	public new SourceParameterInfo[] Parameters { get; set; }

	#endregion
}