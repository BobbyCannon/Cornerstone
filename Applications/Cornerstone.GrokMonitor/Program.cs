#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Presentation.Controls;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor;

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
	public static void Main(string[] args)
	{
		AppBootstrap.Initialize("Cornerstone.GrokMonitor", typeof(Program).Assembly, args);
		BuildCornerstoneApp().UseCornerstone(args).StartWithClassicDesktopLifetime(args);
	}

	#endregion
}
