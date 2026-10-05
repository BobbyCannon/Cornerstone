#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Runtime;

#endregion

namespace CornerstoneApplication.Desktop;

internal static class Program
{
	#region Methods

	/// <summary>
	/// Cornerstone configuration, don't remove; also used by visual designer.
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
		AppBootstrap.Initialize("CornerstoneApplication", typeof(Program).Assembly, args);
		BuildCornerstoneApp().UseCornerstone(args).StartWithClassicDesktopLifetime(args);
	}

	#endregion
}
