using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;

namespace Cornerstone.Presentation.Sandbox;

internal class Program
{
	public static AppBuilder BuildCornerstoneApp()
	{
		return AppBuilder
			.Configure<App>()
			.UsePlatformDetect();
	}

	[STAThread]
	public static void Main(string[] args)
	{
		BuildCornerstoneApp().StartWithClassicDesktopLifetime(args);
	}
}
