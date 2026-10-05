using System;
using System.Diagnostics.CodeAnalysis;

namespace Cornerstone.Presentation.Headless;

/// <summary>
/// Sets up global cornerstone test framework using cornerstone application builder passed as a parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class PresentationTestApplicationAttribute : Attribute
{
    [DynamicallyAccessedMembers(HeadlessUnitTestSession.DynamicallyAccessed)]
    public Type AppBuilderEntryPointType { get; }

    /// <summary>
    /// Creates instance of <see cref="PresentationTestApplicationAttribute"/>. 
    /// </summary>
    /// <param name="appBuilderEntryPointType">
    /// Parameter from which <see cref="AppBuilder"/> should be created.
    /// It either needs to have BuildCornerstoneApp -> AppBuilder method or inherit Application.
    /// </param>
    public PresentationTestApplicationAttribute(
        [DynamicallyAccessedMembers(HeadlessUnitTestSession.DynamicallyAccessed)]
        Type appBuilderEntryPointType)
    {
        AppBuilderEntryPointType = appBuilderEntryPointType;
    }
}
