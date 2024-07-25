#region References

using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Configurations;
using Cornerstone.Avalonia.AvaloniaWebView.Shared.Handlers;
using Cornerstone.Avalonia.Windows.Core;
using PropertyChanging;

#endregion

namespace Cornerstone.Avalonia.Windows;

[DoNotNotify]
[PropertyChanged.DoNotNotify]
public class WebViewHandler : ViewHandler<IVirtualWebView, WebView2Core>
{
	#region Constructors

	public WebViewHandler(IVirtualWebView virtualWebView, IVirtualWebViewControlCallBack callback,
		IVirtualBlazorWebViewProvider provider, WebViewCreationProperties webViewCreationProperties)
	{
		var webView = new WebView2Core(this, callback, provider, webViewCreationProperties);
		PlatformWebView = webView;
		VirtualViewContext = virtualWebView;
		PlatformViewContext = webView;
	}

	#endregion

	#region Methods

	protected override HandleRef CreatePlatformHandler(IPlatformHandle parent, Func<IPlatformHandle> createFromSystem)
	{
		var handler = createFromSystem.Invoke();
		return new HandleRef(this, handler.Handle);
	}

	protected override void Disposing()
	{
		PlatformWebView.Dispose();
		PlatformWebView = default!;
		VirtualViewContext = default!;
	}

	#endregion
}