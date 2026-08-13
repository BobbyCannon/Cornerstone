#region References

using Avalonia;
using Avalonia.Headless;
using Cornerstone.Avalonia;
using Cornerstone.UnitTests;

#endregion

// HeadlessUnitTestSession.GetOrStartForAssembly reads these attributes.
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]
// PerTest resets the dispatcher when another test already touched Avalonia off-session
// (e.g. Terminal constructed without RunOnUi). Slightly slower, much more isolation-safe.
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]

namespace Cornerstone.UnitTests;

/// <summary>
/// Entry point for Avalonia headless unit tests (AvaloniaTestApplicationAttribute).
/// </summary>
public class TestAppBuilder
{
	#region Methods

	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder
			.Configure<TestApplication>()
			.UseSkia()
			.UseHeadless(new AvaloniaHeadlessPlatformOptions
			{
				// Skia path keeps layout/measure closer to real apps; drawing is still headless.
				UseHeadlessDrawing = false
			});
	}

	#endregion

	#region Classes

	public class TestApplication : Application
	{
		#region Methods

		public override void Initialize()
		{
			// HeadlessUnitTestSession uses SetupUnsafe (no OnFrameworkInitializationCompleted).
			// Styles provide control templates; StaticResource keys are promoted in RunOnUi.
			Styles.Add(new CornerstoneTheme());
		}

		#endregion
	}

	#endregion
}
