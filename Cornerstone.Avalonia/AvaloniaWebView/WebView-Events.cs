#region References

using System;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Events;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView;

partial class WebView
{
	#region Events

	public event EventHandler<WebViewFullScreenChangedEventArgs> FullScreenChanged;
	public event EventHandler<WebViewUrlLoadedEventArg> NavigationCompleted;
	public event EventHandler<WebViewUrlLoadingEventArg> NavigationStarting;
	public event EventHandler<WebViewMessageReceivedEventArgs> WebMessageReceived;
	public event EventHandler<WebViewCreatedEventArgs> WebViewCreated;
	public event EventHandler<WebViewCreatingEventArgs> WebViewCreating;
	public event EventHandler<WebViewNewWindowEventArgs> WebViewNewWindowRequested;

	#endregion
}