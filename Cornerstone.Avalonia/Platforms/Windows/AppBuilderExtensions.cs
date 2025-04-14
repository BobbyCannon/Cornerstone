#region References

using System;
using Avalonia;
using Cornerstone.Avalonia.Camera;
using Cornerstone.Avalonia.MediaPlayer;
using Cornerstone.Avalonia.WebView;
using Cornerstone.Runtime;
using Microsoft.Web.WebView2.Core;

#endregion

namespace Cornerstone.Avalonia.Platforms.Windows;

public static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder UseCornerstone(this AppBuilder appBuilder)
	{
		return appBuilder.AfterPlatformServicesSetup(_ =>
		{
			var dependencyProvider = CornerstoneApplication.DependencyProvider;
			dependencyProvider.AddSingleton<ICameraAdapter, CameraAdapter>();
			dependencyProvider.AddSingleton<IMediaPlayerAdapter, MediaPlayerAdapter>();

			if (IsWebView2AvailableInternal())
			{
				dependencyProvider.AddOrUpdateTransient<IWebViewAdapter, WebView2Adapter>();
			}
			else
			{
				dependencyProvider.AddOrUpdateTransient<IWebViewAdapter, WebBrowserAdapter>();
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