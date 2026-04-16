#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Platforms.Windows;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Desktop;

internal class Program
{
	#region Methods

	/// <summary>
	/// Avalonia configuration, don't remove; also used by visual designer.
	/// </summary>
	public static AppBuilder BuildAvaloniaApp()
	{
		var response = AppBuilder
			.Configure<App>()
			.UsePlatformDetect()
			.UseCornerstone()
			.LogToTrace();

		#if DEBUG
		response.AfterSetup(x => x.Instance.AttachDevTools());
		#endif

		return response;
	}

	/// <summary>
	/// Initialization code. Don't use any Avalonia, third-party APIs or any
	/// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	/// yet and stuff might break.
	/// </summary>
	[STAThread]
	public static void Main(string[] args)
	{
		CornerstoneApplication.RuntimeInformation.Initialize(typeof(Program).Assembly);
		CornerstoneApplication.RuntimeInformation.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationName), "Cornerstone.Sample");
		BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	#endregion
}