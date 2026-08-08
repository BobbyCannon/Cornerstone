#region References

using System;
using Avalonia;
using Cornerstone.Avalonia.Platforms;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Documentation;

internal static class Program
{
	#region Methods

	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder
			.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace();
	}

	[STAThread]
	public static void Main(string[] args)
	{
		AppBootstrap.Initialize("Cornerstone.Documentation", typeof(Program).Assembly, args);
		BuildAvaloniaApp().UseCornerstone(args).StartWithClassicDesktopLifetime(args);
	}

	#endregion
}