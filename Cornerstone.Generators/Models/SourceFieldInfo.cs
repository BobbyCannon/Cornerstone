#region References

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceFieldInfo : Reflection.SourceFieldInfo
{
	#region Properties

	public new Accessibility Accessibility { get; set; }
	public new List<SourceAttributeInfo> Attributes { get; } = [];
	public object ConstantValue { get; set; }
	public string FullyQualifiedTypeName { get; set; }
	public new NullableAnnotation NullableAnnotation { get; set; }

	#endregion
}