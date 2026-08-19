#region References

using System;
using Avalonia;
using Cornerstone.Avalonia.Documentation;

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
	public static int Main(string[] args)
	{
		return DocumentationReaderHost.Run(args, new DocumentationReaderHostOptions
		{
			ApplicationName = "Cornerstone.Documentation",
			ApplicationAssembly = typeof(Program).Assembly,
			WindowTitle = "Cornerstone Documentation"
		}, BuildAvaloniaApp());
	}

	#endregion
}