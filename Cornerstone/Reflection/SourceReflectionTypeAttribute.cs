#region References

using System;

#endregion

namespace Cornerstone.Reflection;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class SourceReflectionTypeAttribute(Type type) : Attribute
{
	#region Properties

	public Type Type { get; } = type;

	#endregion
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class SourceReflectionTypeAttribute<T> : Attribute
{
}