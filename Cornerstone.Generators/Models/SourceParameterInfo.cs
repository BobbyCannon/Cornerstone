#region References

using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceParameterInfo : Reflection.SourceParameterInfo
{
	#region Properties

	public string DisplayType { get; set; }
	public bool HasNestedTypeParameter { get; set; }
	public bool IsParameterTypePointer { get; set; }
	public bool IsParameterTypeRefLike { get; set; }
	public bool IsTypeParameter { get; set; }
	public new NullableAnnotation NullableAnnotation { get; set; }
	public ITypeSymbol ParameterSymbol { get; set; }
	public new string ParameterType { get; set; }

	#endregion
}