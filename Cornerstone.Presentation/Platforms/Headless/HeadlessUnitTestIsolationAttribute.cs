using System;

namespace Cornerstone.Presentation.Headless;

/// <summary>
/// Defines the isolation level for headless unit tests,
/// controlling how <see cref="Cornerstone.Presentation.Application"/> and its
/// associated <see cref="Cornerstone.Presentation.Threading.Dispatcher"/> are managed
/// between test runs.
/// </summary>
public enum PresentationTestIsolationLevel
{
    /// <summary>
    /// Reuses a single <see cref="Cornerstone.Presentation.Application"/> and <see cref="Cornerstone.Presentation.Threading.Dispatcher"/>
    /// instance across all tests within the assembly.
    /// </summary>
    /// <remarks>
    /// Tests must not rely on any global or persistent state that could leak between runs.
    /// Headless framework won't dispose any resources after tests when using this mode.
    /// </remarks>
    PerAssembly,

    /// <summary>
    /// Recreates the <see cref="Cornerstone.Presentation.Application"/> and  <see cref="Cornerstone.Presentation.Threading.Dispatcher"/>
    /// for each individual test method.
    /// </summary>
    /// <remarks>
    /// This mode ensures complete test isolation, and should be used for tests that modify global
    /// application state or rely on a clean dispatcher environment.
    /// This is the default isolation level if none is specified.
    /// </remarks>
    PerTest
}

/// <summary>
/// Specifies how headless unit tests should be isolated from each other,
/// defining when the test runtime should recreate the
/// <see cref="Cornerstone.Presentation.Application"/> and <see cref="Cornerstone.Presentation.Threading.Dispatcher"/> instances.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class PresentationTestIsolationAttribute(PresentationTestIsolationLevel isolationLevel) : Attribute
{
    /// <summary>
    /// Gets the isolation level for headless tests.
    /// </summary>
    public PresentationTestIsolationLevel IsolationLevel { get; } = isolationLevel;
}
