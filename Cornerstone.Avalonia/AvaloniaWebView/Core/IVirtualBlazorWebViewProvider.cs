#region References

using System;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Events;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Models;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core;

public interface IVirtualBlazorWebViewProvider
{
	#region Properties

	Uri BaseUri { get; }

	#endregion

	#region Methods

	void PlatformWebViewMessageReceived(object sender, WebViewMessageReceivedEventArgs arg);

	bool PlatformWebViewResourceRequested(object sender, WebResourceRequest request, out WebResourceResponse response);

	bool ResourceRequestedFilterProvider(object requester, out WebScheme filter);

	#endregion
}