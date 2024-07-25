#region References

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Configurations;
using Cornerstone.Avalonia.AvaloniaWebView.Shared.Handlers;
using Microsoft.Web.WebView2.Core;

#endregion

#pragma warning disable CS0414 // Field is assigned but its value is never used

namespace Cornerstone.Avalonia.Windows.Core;

public partial class WebView2Core : IPlatformWebView<WebView2Core>
{
	#region Fields

	private readonly bool _browserCrashed;

	private bool _browserHitTransparent;
	private readonly IVirtualWebViewControlCallBack _callBack;
	private readonly WebViewCreationProperties _creationProperties;
	private readonly ViewHandler _handler;
	private readonly TaskCompletionSource<IntPtr> _hwndTaskSource;
	private bool _isBlazorWebView;
	private bool _isDisposed;
	private bool _isInitialized;
	private readonly IVirtualBlazorWebViewProvider _provider;

	#endregion

	#region Constructors

	public WebView2Core(ViewHandler handler, IVirtualWebViewControlCallBack callback, IVirtualBlazorWebViewProvider provider, WebViewCreationProperties webViewCreationProperties)
	{
		_hwndTaskSource = new();
		_callBack = callback;
		_handler = handler;
		_creationProperties = webViewCreationProperties;
		_browserCrashed = false;
		_isBlazorWebView = false;
		_isInitialized = false;
		_isDisposed = false;
		_provider = provider;

		if (handler.RefHandler.Handle != IntPtr.Zero)
		{
			NativeHandler = handler.RefHandler.Handle;
			_hwndTaskSource.SetResult(handler.RefHandler.Handle);
		}

		SetEnvirmentDefaultBackground(webViewCreationProperties.DefaultWebViewBackgroundColor);
		RegisterEvents();
	}

	~WebView2Core()
	{
		Dispose(false);
	}

	#endregion

	#region Properties

	[Browsable(false)]
	public CoreWebView2 CoreWebView2
	{
		get
		{
			VerifyNotDisposed();
			VerifyBrowserNotCrashed();
			return _coreWebView2Controller?.CoreWebView2;
		}
	}

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

	private CoreWebView2ControllerOptions _controllerOptions { get; set; }
	private CoreWebView2Controller _coreWebView2Controller { get; set; }

	private CoreWebView2Environment _coreWebView2Environment { get; set; }

	#endregion
}