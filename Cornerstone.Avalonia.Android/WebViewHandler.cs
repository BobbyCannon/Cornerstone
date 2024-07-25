#region References

using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Cornerstone.Avalonia.Android.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Configurations;
using Cornerstone.Avalonia.AvaloniaWebView.Shared.Handlers;
using PropertyChanging;

#endregion

namespace Cornerstone.Avalonia.Android;

[DoNotNotify]
[PropertyChanged.DoNotNotify]
public class WebViewHandler : ViewHandler<IVirtualWebView, AndroidWebViewCore>
{
	#region Fields

	private readonly AndroidWebViewCore _webViewCore;

	#endregion

	#region Constructors

	public WebViewHandler(IVirtualWebView virtualWebView, IVirtualWebViewControlCallBack callback, IVirtualBlazorWebViewProvider provider, WebViewCreationProperties webViewCreationProperties)
	{
		var webView = new AndroidWebViewCore(this, callback, provider, webViewCreationProperties);
		_webViewCore = webView;
		PlatformWebView = webView;
		VirtualViewContext = virtualWebView;
		PlatformViewContext = webView;
	}

	#endregion

	#region Methods

	protected override HandleRef CreatePlatformHandler(IPlatformHandle parent, Func<IPlatformHandle> createFromSystem)
	{
		//var handler = createFromSystem.Invoke();
		return new HandleRef(this, _webViewCore.NativeHandler);
	}

	protected override void Disposing()
	{
		PlatformWebView.Dispose();
		PlatformWebView = default!;
		VirtualViewContext = default!;
	}

	#endregion
}