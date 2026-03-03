#region References

using System;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public class SourceParameterInfo
{
	#region Properties

	public object DefaultValue { get; init; }

	public bool HasDefaultValue { get; init; }

	public bool IsOut { get; init; }

	public bool IsRef { get; init; }

	public string Name { get; init; }

	public SourceNullableAnnotation NullableAnnotation { get; init; }

	public Type ParameterType { get; init; }

	#endregion
}