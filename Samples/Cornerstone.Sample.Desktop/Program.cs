#region References

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Avalonia;
using Cornerstone.Sample.ViewModels;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Windows;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Windows;

#endregion

namespace Cornerstone.Sample.Desktop;

public class Program
{
	#region Methods

	/// <summary>
	/// Avalonia configuration, don't remove; also used by visual designer.
	/// </summary>
	public static AppBuilder BuildAvaloniaApp()
	{
		var dependencyProvider = CornerstoneApplication.DependencyProvider;

		WindowsPlatform.Initialize(dependencyProvider);

		var response = AppBuilder
			.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace()
			.UseCornerstoneWindows();

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
		try
		{
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}
		catch (Exception ex)
		{
			Debugger.Break();

			var runtimeInformation = CornerstoneApplication.GetInstance<IRuntimeInformation>();
			var builder = new StringBuilder();

			builder.Append("Crash: ");
			builder.AppendLine(ex.Message);
			builder.AppendLine(ex.StackTrace);

			builder.AppendLine("----------------------------");
			builder.AppendLine(runtimeInformation.ToString());
			builder.AppendLine("----------------------------");

			var directory = Path.Combine(runtimeInformation.ApplicationDataLocation, "CrashLogs");
			new DirectoryInfo(directory).SafeCreate();

			var file = Path.Combine(directory, $"Crash-{DateTime.Now.Ticks:D20}.log");
			File.WriteAllText(file, builder.ToString());

			var viewModel = CornerstoneApplication.GetInstance<MainViewModel>();
			viewModel.Uninitialize();
		}
	}

	#endregion
}