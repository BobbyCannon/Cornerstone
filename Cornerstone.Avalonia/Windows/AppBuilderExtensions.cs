#region References

using System;
using Avalonia;
using Cornerstone.Avalonia.AvaloniaWebView;
using Microsoft.Web.WebView2.Core;

#endregion

namespace Cornerstone.Avalonia.Windows;

public static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseCornerstoneWindows(this AppBuilder appBuilder)
	{
		return appBuilder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = CornerstoneApplication.DependencyProvider;

			if (IsWebView2AvailableInternal())
			{
				dependencyProvider.AddTransient<IWebViewAdapter, WebView2Adapter>();
			}
			else
			{
				dependencyProvider.AddTransient<IWebViewAdapter, WebBrowserAdapter>();
			}
		});
	}

	private static bool IsWebView2AvailableInternal()
	{
		try
		{
			var versionString = CoreWebView2Environment.GetAvailableBrowserVersionString();
			return !string.IsNullOrWhiteSpace(versionString);
		}
		catch (Exception)
		{
			return false;
		}
	}

	#endregion
}