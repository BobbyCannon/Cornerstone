#region References

using System;

#endregion

namespace Cornerstone.Reflection;

/// <summary>
/// Generates an AOT-safe map for a type this assembly does not declare. Same rule as <see cref="SourceReflectionAttribute"/>: compiled accessors, not GetConstructor/Invoke.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class SourceReflectionTypeAttribute(Type type) : Attribute
{
	#region Properties

	public Type Type { get; } = type;

	#endregion
}

/// <summary>
/// Generic form of <see cref="SourceReflectionTypeAttribute"/>. Generated maps must stay AOT-safe.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class SourceReflectionTypeAttribute<T> : Attribute
{
}