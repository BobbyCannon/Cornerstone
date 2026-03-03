#region References

using System;

#endregion

namespace Cornerstone.Reflection;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
public class SourceReflectionAttribute : Attribute
{
}