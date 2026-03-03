#region References

using System;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Instruct Cornerstone.Generators to generate a property which implements INPC using this backing field
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotifyAttribute : CornerstoneAttribute
{
}