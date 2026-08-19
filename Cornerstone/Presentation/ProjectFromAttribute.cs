#region References

using System;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Generates destination-bag properties on this ViewModel from a shared contract
/// (same names and types as State). Author file keeps lists, commands, and
/// presentation-only members.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ProjectFromAttribute<TContract> : CornerstoneAttribute
	where TContract : class
{
}
