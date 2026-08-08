#region References

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Cornerstone.Avalonia.Platforms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests;

[TestClass]
public class CornerstoneAvaloniaUnitTest : CornerstoneUnitTest
{
	#region Fields

	private static bool _isPlatformInitialized;

	#endregion

	#region Methods

	[AssemblyInitialize]
	public static void AssemblyInitialize(TestContext context)
	{
		if (!_isPlatformInitialized)
		{
			TestAppBuilder
				.BuildAvaloniaApp()
				.UseSkia()
				.UseHeadless(new AvaloniaHeadlessPlatformOptions())
				.SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime())
				.UseCornerstone([]);

			_isPlatformInitialized = true;
		}
	}

	#endregion
}