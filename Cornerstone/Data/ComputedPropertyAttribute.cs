#region References

using System;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Attribute for computed properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ComputedPropertyAttribute : CornerstoneAttribute
{
}