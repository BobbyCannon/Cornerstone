#region References

using Avalonia;
using Avalonia.Headless;
using Cornerstone.Avalonia;
using Cornerstone.UnitTests;

#endregion

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Cornerstone.UnitTests;

public class TestAppBuilder
{
	#region Methods

	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder
			.Configure<TestApplication>();
	}

	#endregion

	#region Classes

	public class TestApplication : Application
	{
		#region Constructors

		public TestApplication()
		{
			Styles.Add(new CornerstoneTheme());
		}

		#endregion

		#region Methods

		public static AppBuilder BuildAvaloniaApp()
		{
			return AppBuilder
				.Configure<TestApplication>()
				.UseSkia()
				.UseHeadless(new AvaloniaHeadlessPlatformOptions
				{
					UseHeadlessDrawing = false
				});
		}

		#endregion
	}

	#endregion
}