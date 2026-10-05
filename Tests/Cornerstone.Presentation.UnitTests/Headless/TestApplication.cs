#region References

using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Headless;

[TestClass]
public class TestApplication : Application
{
	#region Constructors

	public TestApplication()
	{
		Styles.Add(new CornerstoneTheme());
	}

	#endregion

	#region Properties

	/// <summary>
	/// Enabled by the PerTest projects only, so that both mouse device modes are covered.
	/// </summary>
	public static bool UsesSharedMouseDevice =>
		#if PERTEST
		true;
	#else
		false;
	#endif

	#endregion

	#region Methods

	public static AppBuilder BuildCornerstoneApp()
	{
		return AppBuilder.Configure<TestApplication>()
			.UseHarfBuzz()
			.UseSkia()
			.UseHeadless(new PresentationHeadlessPlatformOptions
			{
				UseHeadlessDrawing = false,
				OverlayPopups = false,
				UseSharedMouseDevice = UsesSharedMouseDevice
			})
			.AfterPlatformServicesSetup(_ => PresentationLocator.CurrentMutable
				.Bind<IGlobalClock>().ToConstant(new MockGlobalClock())
				.Bind<CornerstoneXamlLoader.IRuntimeXamlLoader>().ToConstant(new TestRuntimeXamlLoader()));
	}

	#endregion
}