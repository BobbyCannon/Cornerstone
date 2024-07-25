#region References

using Cornerstone.Avalonia.AvaloniaWebView.Core;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView;

partial class WebView
{
	#region Properties

	IPlatformWebView IVirtualWebView.PlatformView => PlatformWebView;

	WebView IVirtualWebView<WebView>.VirtualView => this;

	object IVirtualWebView.VirtualViewObject => this;

	#endregion
}