#region References

using System;
using Cornerstone.MediaPlayer;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.MediaPlayer.Desktop;

internal static class Program
{
	#region Methods

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

	[STAThread]
	public static void Main(string[] args)
	{
		AppBootstrap.Initialize("Cornerstone.MediaPlayer", typeof(Program).Assembly, args);
		BuildCornerstoneApp().UseCornerstone(args).StartWithClassicDesktopLifetime(args);
	}

	#endregion
}
