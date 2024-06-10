#region References

using System;

#endregion

namespace Cornerstone.Attributes;

/// <summary>
/// Attribute for flagging the primary dependency injection constructor.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
public class DependencyInjectionConstructorAttribute : CornerstoneAttribute
{
}