#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Theme;
using Cornerstone.UnitTests;

#endregion

// HeadlessUnitTestSession.GetOrStartForAssembly reads these attributes.
[assembly: PresentationTestApplication(typeof(TestAppBuilder))]

// PerTest resets the dispatcher when another test already touched Cornerstone off-session
// (e.g. Terminal constructed without RunOnUi). Slightly slower, much more isolation-safe.
[assembly: PresentationTestIsolation(PresentationTestIsolationLevel.PerTest)]

namespace Cornerstone.UnitTests;

/// <summary>
/// Entry point for Cornerstone headless unit tests (PresentationTestApplicationAttribute).
/// </summary>
public class TestAppBuilder
{
	#region Methods

	public static AppBuilder BuildCornerstoneApp()
	{
		return AppBuilder
			.Configure<TestApplication>()
			.UseSkia()
			.UseHeadless(new PresentationHeadlessPlatformOptions
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