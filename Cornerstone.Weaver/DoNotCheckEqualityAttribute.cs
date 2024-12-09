#region References

using System;

#endregion

namespace Cornerstone.Weaver;

/// <summary>
/// Skip equality check before change notification
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
public class DoNotCheckEqualityAttribute : Attribute;