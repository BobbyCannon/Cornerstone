#region References

using System;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Attribute for flagging a property for dependency.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DependencyInjectedPropertyAttribute : CornerstoneAttribute
{
}