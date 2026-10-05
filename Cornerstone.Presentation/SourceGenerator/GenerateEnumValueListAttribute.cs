#region References

using System;

#endregion

namespace Cornerstone.Presentation.SourceGenerator;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class GenerateEnumValueListAttribute : Attribute
{
}