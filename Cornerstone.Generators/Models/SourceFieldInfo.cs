#region References

using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceFieldInfo : SourceMemberInfo
{
	#region Properties

	public Accessibility Accessibility { get; set; }
	public object ConstantValue { get; set; }
	public string FullyQualifiedTypeName { get; set; }
	public bool IsConstant { get; set; }
	public bool IsReadOnly { get; set; }
	public bool IsRequired { get; set; }
	public bool IsStatic { get; set; }
	public NullableAnnotation NullableAnnotation { get; set; }

	#endregion
}