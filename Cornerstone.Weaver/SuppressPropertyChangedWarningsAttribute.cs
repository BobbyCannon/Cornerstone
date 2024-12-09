#region References

using System;

#endregion

namespace Cornerstone.Weaver;

/// <summary>
/// Suppresses warnings emitted by Cornerstone.Weaver.Fody
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class SuppressPropertyChangedWarningsAttribute : Attribute;