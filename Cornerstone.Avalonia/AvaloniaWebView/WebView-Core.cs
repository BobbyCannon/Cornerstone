#region References

using System;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView;

partial class WebView
{
	#region Methods

	private async Task<bool> Navigate(Uri uri)
	{
		if (uri is null)
		{
			return false;
		}

		if (PlatformWebView is null)
		{
			return false;
		}

		if (!PlatformWebView.IsInitialized)
		{
			var bRet = await PlatformWebView.Initialize();
			if (!bRet)
			{
				return false;
			}
		}

		if (PlatformWebView is null)
		{
			return false;
		}

		try
		{
			return PlatformWebView.Navigate(uri);
		}
		catch
		{
			return false;
		}
	}

	private async Task<bool> NavigateToString(string htmlContent)
	{
		if (string.IsNullOrWhiteSpace(htmlContent))
		{
			return false;
		}

		if (PlatformWebView is null)
		{
			return false;
		}

		if (!PlatformWebView.IsInitialized)
		{
			var bRet = await PlatformWebView.Initialize();
			if (!bRet)
			{
				return false;
			}
		}

		return PlatformWebView.NavigateToString(htmlContent!);
	}

	#endregion
}