#region References

using Cornerstone.Avalonia.AvaloniaWebView.Core.Events;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core;

public interface IVirtualWebViewControlCallBack
{
	#region Methods

	void PlatformWebViewCreated(object sender, WebViewCreatedEventArgs arg);

	bool PlatformWebViewCreating(object sender, WebViewCreatingEventArgs arg);

	void PlatformWebViewFullScreenChanged(object sender, WebViewFullScreenChangedEventArgs args);

	void PlatformWebViewMessageReceived(object sender, WebViewMessageReceivedEventArgs arg);

	void PlatformWebViewNavigationCompleted(object sender, WebViewUrlLoadedEventArg arg);

	bool PlatformWebViewNavigationStarting(object sender, WebViewUrlLoadingEventArg arg);

	bool PlatformWebViewNewWindowRequest(object sender, WebViewNewWindowEventArgs arg);

	#endregion
}