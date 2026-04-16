#region References

using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceAttributeInfo : Reflection.SourceAttributeInfo
{
	#region Properties

	public AttributeData Data { get; set; }
	public INamedTypeSymbol TypeSymbol { get; set; }

	#endregion
}