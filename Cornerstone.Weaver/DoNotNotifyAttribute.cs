#region References

using System;

#endregion

namespace Cornerstone.Weaver;

/// <summary>
/// Exclude a <see cref="Type" /> or property from notification.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
public class DoNotNotifyAttribute : Attribute;