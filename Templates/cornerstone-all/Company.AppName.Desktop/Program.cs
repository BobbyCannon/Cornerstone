#region References

using System;
using Company.AppName;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName.Desktop;

internal static class Program
{
	#region Methods

	/// <summary>
	/// Cornerstone configuration; also used by the visual designer.
	/// </summary>
	public static AppBuilder BuildCornerstoneApp()
	{
		return AppBuilder
			.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace();
	}

	[STAThread]
	public static void Main(string[] args)
	{
		AppBootstrap.Initialize("Company.AppName", typeof(Program).Assembly, args);
		BuildCornerstoneApp().UseCornerstone(args).StartWithClassicDesktopLifetime(args);
	}

	#endregion
}
