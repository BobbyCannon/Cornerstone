#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ThemeVariantTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ApplicationActualThemeVariantFallsBackToLightWithoutPlatformSettings()
	{
		var application = new Application();
		application.InitializeThemeVariant();

		CornerstoneTest.AreEqual(ThemeVariant.Light, application.ActualThemeVariant);
	}

	[PresentationTestMethod]
	public void ApplicationActualThemeVariantFollowsPlatformSettingsChanges()
	{
		var platformSettings = new TestPlatformSettings(PlatformThemeVariant.Light);
		using var app = StartApplication(platformSettings);
		var application = Application.Current!;

		var raised = 0;
		application.ActualThemeVariantChanged += (_, _) => ++raised;

		platformSettings.ThemeVariant = PlatformThemeVariant.Dark;

		CornerstoneTest.AreEqual(ThemeVariant.Dark, application.ActualThemeVariant);
		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	[DataRow(PlatformThemeVariant.Light)]
	[DataRow(PlatformThemeVariant.Dark)]
	public void ApplicationActualThemeVariantIsInitializedFromPlatformSettings(
		PlatformThemeVariant platformThemeVariant)
	{
		using var app = StartApplication(new TestPlatformSettings(platformThemeVariant));

		CornerstoneTest.AreEqual((ThemeVariant) platformThemeVariant, Application.Current!.ActualThemeVariant);
	}

	[PresentationTestMethod]
	public void ApplicationActualThemeVariantRevertsToPlatformSettingsWhenRequestingDefault()
	{
		var platformSettings = new TestPlatformSettings(PlatformThemeVariant.Dark);
		using var app = StartApplication(platformSettings);
		var application = Application.Current!;

		application.RequestedThemeVariant = ThemeVariant.Light;
		CornerstoneTest.AreEqual(ThemeVariant.Light, application.ActualThemeVariant);

		application.RequestedThemeVariant = ThemeVariant.Default;
		CornerstoneTest.AreEqual(ThemeVariant.Dark, application.ActualThemeVariant);
	}

	[PresentationTestMethod]
	public void ApplicationActualThemeVariantRevertsToPlatformSettingsWhenRequestingNull()
	{
		var platformSettings = new TestPlatformSettings(PlatformThemeVariant.Dark);
		using var app = StartApplication(platformSettings);
		var application = Application.Current!;

		application.RequestedThemeVariant = ThemeVariant.Light;
		CornerstoneTest.AreEqual(ThemeVariant.Light, application.ActualThemeVariant);

		application.RequestedThemeVariant = null;
		CornerstoneTest.AreEqual(ThemeVariant.Dark, application.ActualThemeVariant);
	}

	[PresentationTestMethod]
	public void ApplicationCustomThemeVariantIsUsedAsIs()
	{
		var custom = new ThemeVariant("Custom", ThemeVariant.Dark);
		using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Light));
		var application = Application.Current!;

		application.RequestedThemeVariant = custom;

		CornerstoneTest.AreEqual(custom, application.ActualThemeVariant);
	}

	[PresentationTestMethod]
	public void ApplicationRequestedThemeVariantOverridesPlatformSettings()
	{
		var platformSettings = new TestPlatformSettings(PlatformThemeVariant.Light);
		using var app = StartApplication(platformSettings);
		var application = Application.Current!;

		application.RequestedThemeVariant = ThemeVariant.Dark;

		CornerstoneTest.AreEqual(ThemeVariant.Dark, application.ActualThemeVariant);

		platformSettings.ThemeVariant = PlatformThemeVariant.Light;
		CornerstoneTest.AreEqual(ThemeVariant.Dark, application.ActualThemeVariant);
	}

	[PresentationTestMethod]
	public void ThemeVariantScopeRequestingDefaultReinheritsFromParent()
	{
		using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Light));

		BindApplicationAsThemeVariantHost();
		Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;

		var child = new Border();
		var scope = new ThemeVariantScope { Child = child };
		var window = new Window { Content = scope };
		window.Show();

		scope.RequestedThemeVariant = ThemeVariant.Light;
		CornerstoneTest.AreEqual(ThemeVariant.Light, scope.ActualThemeVariant);
		CornerstoneTest.AreEqual(ThemeVariant.Light, child.ActualThemeVariant);

		scope.RequestedThemeVariant = ThemeVariant.Default;
		CornerstoneTest.AreEqual(ThemeVariant.Dark, scope.ActualThemeVariant);
		CornerstoneTest.AreEqual(ThemeVariant.Dark, child.ActualThemeVariant);
	}

	[PresentationTestMethod]
	public void TopLevelActualThemeVariantFollowsApplication()
	{
		var platformSettings = new TestPlatformSettings(PlatformThemeVariant.Light);
		using var app = StartApplication(platformSettings);
		var application = Application.Current!;

		BindApplicationAsThemeVariantHost();

		var window = new Window();
		CornerstoneTest.AreEqual(ThemeVariant.Light, window.ActualThemeVariant);

		application.RequestedThemeVariant = ThemeVariant.Dark;
		CornerstoneTest.AreEqual(ThemeVariant.Dark, window.ActualThemeVariant);

		application.RequestedThemeVariant = ThemeVariant.Default;
		CornerstoneTest.AreEqual(ThemeVariant.Light, window.ActualThemeVariant);

		platformSettings.ThemeVariant = PlatformThemeVariant.Dark;
		CornerstoneTest.AreEqual(ThemeVariant.Dark, window.ActualThemeVariant);
	}

	[PresentationTestMethod]
	public void TopLevelActualThemeVariantIsInheritedByChildren()
	{
		using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Dark));

		BindApplicationAsThemeVariantHost();

		var child = new Border();
		var scope = new ThemeVariantScope { Child = child };
		var window = new Window { Content = scope };
		window.Show();

		CornerstoneTest.AreEqual(ThemeVariant.Dark, scope.ActualThemeVariant);
		CornerstoneTest.AreEqual(ThemeVariant.Dark, child.ActualThemeVariant);

		scope.RequestedThemeVariant = ThemeVariant.Light;

		CornerstoneTest.AreEqual(ThemeVariant.Dark, window.ActualThemeVariant);
		CornerstoneTest.AreEqual(ThemeVariant.Light, scope.ActualThemeVariant);
		CornerstoneTest.AreEqual(ThemeVariant.Light, child.ActualThemeVariant);
	}

	[PresentationTestMethod]
	public void TopLevelActualThemeVariantIsInitializedFromApplication()
	{
		using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Light));

		BindApplicationAsThemeVariantHost();
		Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;

		var window = new Window();

		CornerstoneTest.AreEqual(ThemeVariant.Dark, window.ActualThemeVariant);
	}

	[PresentationTestMethod]
	public void TopLevelActualThemeVariantIsInitializedFromPlatformSettingsWithoutThemeVariantHost()
	{
		using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Dark));

		var window = new Window();

		CornerstoneTest.AreEqual(ThemeVariant.Dark, window.ActualThemeVariant);
	}

	[PresentationTestMethod]
	public void TopLevelRequestedThemeVariantOverridesApplicationAndIsSentToPlatformImpl()
	{
		using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Dark));

		BindApplicationAsThemeVariantHost();

		var window = new Window { RequestedThemeVariant = ThemeVariant.Light };

		CornerstoneTest.AreEqual(ThemeVariant.Dark, Application.Current!.ActualThemeVariant);
		CornerstoneTest.AreEqual(ThemeVariant.Light, window.ActualThemeVariant);
	}

	private static void BindApplicationAsThemeVariantHost()
	{
		PresentationLocator.CurrentMutable.Bind<IThemeVariantHost>().ToConstant(Application.Current!);
	}

	private static IDisposable StartApplication(TestPlatformSettings platformSettings)
	{
		return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(
			platformSettings: platformSettings,
			windowingPlatform: new MockWindowingPlatform(),
			standardCursorFactory: new StubCursorFactory()));
	}

	#endregion
}