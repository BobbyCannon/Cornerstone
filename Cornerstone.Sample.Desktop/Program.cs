#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Presentation.Controls;
using Cornerstone.Esri;
using Cornerstone.Vlc;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample;

#endregion

namespace Cornerstone.Sample.Desktop;

internal class Program
{
	#region Methods

	/// <summary>
	/// Cornerstone configuration, don't remove; also used by visual designer.
	/// </summary>
	public static AppBuilder BuildCornerstoneApp()
	{
		var response = AppBuilder
			.Configure<App>()
			.With(new Win32PlatformOptions { NativeBehindComposition = SampleNativeAirspace.IsEnabled })
			.With(new MacOSPlatformOptions { NativeBehindComposition = SampleNativeAirspace.IsEnabled })
			.With(new WaylandPlatformOptions { NativeBehindComposition = SampleNativeAirspace.IsEnabled })
			.UsePlatformDetect()
			.LogToTrace();

		#if DEBUG
		response.AfterSetup(x => x.Instance.AttachDevTools());
		#endif

		return response;
	}

	/// <summary>
	/// Initialization code. Don't use any Cornerstone, third-party APIs or any
	/// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	/// yet and stuff might break.
	/// </summary>
	[STAThread]
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Remote display BSON is a desktop debug/host path, not a trimmed publish.")]
	public static void Main(string[] args)
	{
		AppBootstrap.StartupProfiler ??= new StartupProfiler();
		AppBootstrap.Initialize("Cornerstone.Sample", typeof(Program).Assembly, args);
		BuildCornerstoneApp()
			.UseCornerstone(args)
			.UseCornerstoneEsri()
			.UseCornerstoneVlc()
			.UseRemoteDisplay(args)
			.StartWithClassicDesktopLifetime(args);
	}

	#endregion
}
