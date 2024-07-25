#region References

using System;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Configurations;
using Cornerstone.Avalonia.AvaloniaWebView.Shared;

#endregion

namespace Cornerstone.Avalonia.Android;

internal class ViewHandlerProvider : IViewHandlerProvider
{
	#region Methods

	IViewHandler IViewHandlerProvider.CreatePlatformWebViewHandler(IVirtualWebView virtualView, IVirtualWebViewControlCallBack virtualViewCallBack, IVirtualBlazorWebViewProvider provider, Action<WebViewCreationProperties> configDelegate)
	{
		var creationProperty = new WebViewCreationProperties();
		configDelegate?.Invoke(creationProperty);

		return new WebViewHandler(virtualView, virtualViewCallBack, provider, creationProperty);
	}

	#endregion
}