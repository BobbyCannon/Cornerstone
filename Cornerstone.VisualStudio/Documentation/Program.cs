#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Presentation.Documentation;

#endregion

namespace Cornerstone.VisualStudio.Documentation;

internal static class Program
{
	#region Methods

	public static AppBuilder BuildCornerstoneApp()
	{
		return AppBuilder
			.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace();
	}

	[STAThread]
	public static int Main(string[] args)
	{
		return DocumentationReaderHost.Run(args, new DocumentationReaderHostOptions
		{
			ApplicationName = "Cornerstone.VisualStudio.Documentation",
			ApplicationAssembly = typeof(Program).Assembly,
			WindowTitle = "Cornerstone Visual Studio Documentation",
			WindowIcon = "/Assets/Cornerstone.ico"
		}, BuildCornerstoneApp());
	}

	#endregion
}
