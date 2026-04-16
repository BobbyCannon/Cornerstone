#region References

using System;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public class SourceParameterInfo
{
	#region Properties

	public object DefaultValue { get; set; }
	public bool HasDefaultValue { get; set; }
	public bool IsOut { get; set; }
	public bool IsRef { get; set; }
	public string Name { get; set; }
	public SourceNullableAnnotation NullableAnnotation { get; set; }
	public Type ParameterType { get; set; }

	#endregion
}