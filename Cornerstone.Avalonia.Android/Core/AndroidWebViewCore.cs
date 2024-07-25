#region References

using System.Threading;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Configurations;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Events;
using Cornerstone.Avalonia.AvaloniaWebView.Shared.Handlers;

#endregion

#pragma warning disable CS0414 // Field is assigned but its value is never used

namespace Cornerstone.Avalonia.Android.Core;

public partial class AndroidWebViewCore : IPlatformWebView<AndroidWebViewCore>
{
	#region Fields

	private readonly IVirtualWebViewControlCallBack _callBack;
	private readonly WebViewCreationProperties _creationProperties;
	private readonly ViewHandler _handler;
	private bool _isBlazorWebView;
	private bool _isDisposed;
	private bool _isInitialized;
	private readonly IVirtualBlazorWebViewProvider _provider;
	private WebChromeClient _webChromeClient;
	private WebViewClient _webViewClient;

	#endregion

	#region Constructors

	public AndroidWebViewCore(ViewHandler handler, IVirtualWebViewControlCallBack callback, IVirtualBlazorWebViewProvider provider, WebViewCreationProperties webViewCreationProperties)
	{
		_provider = provider;
		_callBack = callback;
		_handler = handler;
		_creationProperties = webViewCreationProperties;

		_callBack.PlatformWebViewCreating(this, new WebViewCreatingEventArgs());
		AndroidWebView.SetWebContentsDebuggingEnabled(webViewCreationProperties.AreDevToolEnabled);

		var parentContext = AndroidApplication.Context;
		var webView = new AndroidWebView(parentContext)
		{
			#pragma warning disable CS0618, CA1422 // Type or member is obsolete // Validate platform compatibility
			LayoutParameters = new AbsoluteLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent, 0, 0),
			#pragma warning restore CS0618, CA1422 // Type or member is obsolete
		};
		webView.SetBackgroundColor(new AndroidColor(webViewCreationProperties.DefaultWebViewBackgroundColor.R, webViewCreationProperties.DefaultWebViewBackgroundColor.G, webViewCreationProperties.DefaultWebViewBackgroundColor.B));

		WebView = webView;
		NativeHandler = webView.Handle;
		RegisterEvents();
	}

	~AndroidWebViewCore()
	{
		Dispose(false);
	}

	#endregion

	#region Properties

	public bool IsDisposed
	{
		get => Volatile.Read(ref _isDisposed);
		private set => Volatile.Write(ref _isDisposed, value);
	}

	public bool IsInitialized
	{
		get => Volatile.Read(ref _isInitialized);
		private set => Volatile.Write(ref _isInitialized, value);
	}

	public AndroidWebView WebView { get; set; }

	#endregion
}