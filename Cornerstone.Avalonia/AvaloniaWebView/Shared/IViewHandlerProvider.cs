#region References

using System;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Configurations;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Shared;

public interface IViewHandlerProvider
{
	#region Methods

	IViewHandler CreatePlatformWebViewHandler(IVirtualWebView virtualView, IVirtualWebViewControlCallBack virtualViewCallBack, IVirtualBlazorWebViewProvider virtualBlazorWebViewCallBack, Action<WebViewCreationProperties> configDelegate = default);

	#endregion
}