#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName;

internal static class Program
{
	#region Methods

	/// <summary>
	/// Builds the Cornerstone application. The visual designer uses this too.
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
