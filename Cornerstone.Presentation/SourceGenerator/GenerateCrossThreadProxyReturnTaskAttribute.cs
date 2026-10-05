#region References

using System;

#endregion

namespace Cornerstone.Presentation.SourceGenerator;

/// <summary>
/// When applied to a void-returning method on an interface marked with
/// <see cref="GenerateCrossThreadProxyAttribute" />, the generated proxy
/// returns <see cref="System.Threading.Tasks.Task" /> instead of being
/// fire-and-forget. Has no effect on non-void methods (which are always
/// wrapped into <see cref="System.Threading.Tasks.Task{TResult}" />).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class GenerateCrossThreadProxyReturnTaskAttribute : Attribute
{
}