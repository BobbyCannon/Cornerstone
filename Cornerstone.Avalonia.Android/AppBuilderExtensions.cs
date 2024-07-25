#region References

using Avalonia;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Shared;

#endregion

namespace Cornerstone.Avalonia.Android;

public static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseAndroidWebView(this AppBuilder builder)
	{
		return builder.AfterPlatformServicesSetup(app =>
		{
			CornerstoneApplication.PlatformDependencies.AddSingleton<IViewHandlerProvider, ViewHandlerProvider>();
			CornerstoneApplication.PlatformDependencies.AddSingleton<IPlatformBlazorWebViewProvider, BlazorWebViewHandlerProvider>();
		});
	}

	#endregion
}