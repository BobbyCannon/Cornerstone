#region References

using Avalonia;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Shared;

#endregion

namespace Cornerstone.Avalonia.Windows;

public static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseWindowWebView(this AppBuilder appBuilder)
	{
		return appBuilder.AfterPlatformServicesSetup(app =>
		{
			CornerstoneApplication.PlatformDependencies.AddSingleton<IViewHandlerProvider, ViewHandlerProvider>();
			CornerstoneApplication.PlatformDependencies.AddSingleton<IPlatformBlazorWebViewProvider, BlazorWebViewHandlerProvider>();
		});
	}

	#endregion
}