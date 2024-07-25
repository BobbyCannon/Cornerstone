#region References

using System;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Events;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core;

public interface IWebViewEventHandler
{
	#region Events

	event EventHandler<WebViewUrlLoadedEventArg> NavigationCompleted;

	event EventHandler<WebViewUrlLoadingEventArg> NavigationStarting;

	event EventHandler<WebViewMessageReceivedEventArgs> WebMessageReceived;
	event EventHandler<WebViewCreatedEventArgs> WebViewCreated;
	event EventHandler<WebViewCreatingEventArgs> WebViewCreating;
	event EventHandler<WebViewNewWindowEventArgs> WebViewNewWindowRequested;

	#endregion
}