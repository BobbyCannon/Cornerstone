#region References

using System;
using Avalonia;
using Cornerstone.Avalonia.Platforms;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName;

internal static class Program
{
	#region Methods

	/// <summary>
	/// Avalonia configuration; also used by the visual designer.
	/// </summary>
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
		AppBootstrap.Initialize("Company.AppName", typeof(Program).Assembly, args);
		BuildAvaloniaApp().UseCornerstone(args).StartWithClassicDesktopLifetime(args);
	}

	#endregion
}
