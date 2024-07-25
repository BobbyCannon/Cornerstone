#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Avalonia.AvaloniaWebView.Core;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView;

partial class WebView
{
	#region Properties

	public bool IsCanGoBack => PlatformWebView?.IsCanGoBack ?? false;

	public bool IsCanGoForward => PlatformWebView?.IsCanGoForward ?? false;

	#endregion

	#region Methods

	public Task<string> ExecuteScriptAsync(string javaScript)
	{
		if (PlatformWebView is null || !PlatformWebView.IsInitialized)
		{
			return Task.FromResult<string>(default);
		}

		return PlatformWebView.ExecuteScriptAsync(javaScript);
	}

	public bool GoBack()
	{
		if (PlatformWebView is null || !PlatformWebView.IsInitialized)
		{
			return false;
		}

		return PlatformWebView.GoBack();
	}

	public bool GoForward()
	{
		if (PlatformWebView is null || !PlatformWebView.IsInitialized)
		{
			return false;
		}

		return PlatformWebView.GoForward();
	}

	public bool OpenDevToolsWindow()
	{
		if (PlatformWebView is null || !PlatformWebView.IsInitialized)
		{
			return false;
		}

		return PlatformWebView.OpenDevToolsWindow();
	}

	public bool PostWebMessageAsJson(string webMessageAsJson, Uri baseUri)
	{
		if (PlatformWebView is null || !PlatformWebView.IsInitialized)
		{
			return false;
		}

		return PlatformWebView.PostWebMessageAsString(webMessageAsJson, baseUri);
	}

	public bool PostWebMessageAsString(string webMessageAsString, Uri baseUri)
	{
		if (PlatformWebView is null || !PlatformWebView.IsInitialized)
		{
			return false;
		}

		return PlatformWebView.PostWebMessageAsString(webMessageAsString, baseUri);
	}

	public bool Reload()
	{
		if (PlatformWebView is null || !PlatformWebView.IsInitialized)
		{
			return false;
		}

		return PlatformWebView.Reload();
	}

	public bool Stop()
	{
		if (PlatformWebView is null || !PlatformWebView.IsInitialized)
		{
			return false;
		}

		return PlatformWebView.Stop();
	}

	bool IWebViewControl.Navigate(Uri uri)
	{
		if (uri is null)
		{
			return false;
		}

		if (PlatformWebView is null || !PlatformWebView.IsInitialized)
		{
			return false;
		}

		return PlatformWebView.Navigate(uri);
	}

	bool IWebViewControl.NavigateToString(string htmlContent)
	{
		if (string.IsNullOrWhiteSpace(htmlContent))
		{
			return false;
		}

		if (PlatformWebView is null || !PlatformWebView.IsInitialized)
		{
			return false;
		}

		return PlatformWebView.NavigateToString(htmlContent);
	}

	#endregion
}