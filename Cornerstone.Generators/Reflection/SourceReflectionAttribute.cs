#region References

using System;

#endregion

namespace Cornerstone.Reflection;

/// <summary>
/// Generates an AOT-safe type map. That is the purpose of source reflection: compiled construct/get/set, not runtime member lookup.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
public class SourceReflectionAttribute : Attribute
{
}