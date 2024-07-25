#region References

using System.Threading.Tasks;
using Cornerstone.Avalonia.Android.Clients;
using Cornerstone.Avalonia.AvaloniaWebView.Core;

#endregion

namespace Cornerstone.Avalonia.Android.Core;

partial class AndroidWebViewCore
{
	#region Methods

	private void ClearBlazorWebViewCompleted()
	{
		_isBlazorWebView = false;
	}

	private Task<bool> PrepareBlazorWebViewStarting(AndroidWebView webView, IVirtualBlazorWebViewProvider provider)
	{
		if (webView is null)
		{
			return Task.FromResult(false);
		}

		if (provider is null)
		{
			return Task.FromResult(false);
		}

		if (!provider.ResourceRequestedFilterProvider(this, out var filter))
		{
			return Task.FromResult(false);
		}

		_webViewClient = new AvaloniaWebViewClient(this, _callBack, provider, filter);
		_webChromeClient = new AvaloniaWebChromeClient(this);
		webView.SetWebViewClient(_webViewClient);
		webView.SetWebChromeClient(_webChromeClient);
		_isBlazorWebView = true;
		return Task.FromResult(true);
	}

	#endregion
}