#region References

using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceParameterInfo
{
	#region Properties

	public object DefaultValue { get; set; }
	public string DisplayType { get; set; }
	public bool HasDefaultValue { get; set; }
	public bool HasNestedTypeParameter { get; set; }
	public bool IsOut { get; set; }
	public bool IsParameterTypePointer { get; set; }
	public bool IsParameterTypeRefLike { get; set; }
	public bool IsRef { get; set; }
	public bool IsTypeParameter { get; set; }
	public string Name { get; set; }
	public NullableAnnotation NullableAnnotation { get; set; }
	public ITypeSymbol ParameterSymbol { get; set; }
	public string ParameterType { get; set; }

	#endregion
}