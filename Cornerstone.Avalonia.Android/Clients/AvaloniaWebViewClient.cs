#region References

using System;
using System.Runtime.Versioning;
using Android.Content;
using Android.Runtime;
using Android.Webkit;
using Cornerstone.Avalonia.Android.Core;
using Cornerstone.Avalonia.Android.Handlers;
using Cornerstone.Avalonia.Android.Helpers;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Enums;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Events;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Extensions;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Helpers;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Models;

#endregion

namespace Cornerstone.Avalonia.Android.Clients;

#pragma warning disable CS0414 // Field is assigned but its value is never used

[SupportedOSPlatform("android23.0")]
internal class AvaloniaWebViewClient : WebViewClient
{
	#region Fields

	private readonly IVirtualWebViewControlCallBack _callBack;
	private bool _isStarted;
	private readonly IVirtualBlazorWebViewProvider _provider;
	private readonly WebScheme _webScheme;
	private readonly AndroidWebView _webView;
	private readonly AndroidWebViewCore _webViewCore;

	#endregion

	#region Constructors

	public AvaloniaWebViewClient(AndroidWebViewCore webViewHandler, IVirtualWebViewControlCallBack callBack, IVirtualBlazorWebViewProvider provider, WebScheme webScheme)
	{
		ArgumentNullException.ThrowIfNull(webViewHandler);
		ArgumentNullException.ThrowIfNull(callBack);
		ArgumentNullException.ThrowIfNull(provider);
		ArgumentNullException.ThrowIfNull(webScheme);
		_callBack = callBack;
		_webViewCore = webViewHandler;
		_provider = provider;
		_webView = webViewHandler.WebView;
		_webScheme = webScheme;
	}

	protected AvaloniaWebViewClient(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
	{
		// This constructor is called whenever the .NET proxy was disposed, and it was recreated by Java. It also
		// happens when overridden methods are called between execution of this constructor and the one above.
		// because of these facts, we have to check all methods below for null field references and properties.
	}

	#endregion

	#region Methods

	public override void OnPageFinished(AndroidWebView view, string url)
	{
		base.OnPageFinished(view, url);

		if (view is null)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(url))
		{
			return;
		}

		if (_webScheme is null)
		{
			return;
		}

		if (_webScheme.BaseUri.IsBaseOfPage(url))
		{
			RunBlazorStartUpScripts();
		}
	}

	public override AndroidWebResourceResponse ShouldInterceptRequest(AndroidWebView view, IWebResourceRequest request)
	{
		ArgumentException.ThrowIfNullOrEmpty(nameof(request));
		var func = () =>
		{
			if (_webScheme is null || _provider is null)
			{
				return default;
			}

			var requestUri = request?.Url?.ToString();
			if (requestUri == null)
			{
				return default;
			}

			var allowFallbackOnHostPage = _webScheme.BaseUri.IsBaseOfPage(requestUri);

			var webRequest = new WebResourceRequest
			{
				RequestUri = requestUri!,
				AllowFallbackOnHostPage = allowFallbackOnHostPage
			};

			if (!_provider.PlatformWebViewResourceRequested(_webViewCore, webRequest, out var webResponse))
			{
				return default;
			}

			if (webResponse is null)
			{
				return default;
			}

			var contentType = webResponse.Headers[QueryStringHelper.ContentTypeKey];

			return new AndroidWebResourceResponse(contentType, "UTF-8", webResponse.StatusCode, webResponse.StatusMessage, webResponse.Headers, webResponse.Content);
		};
		var ret = func.Invoke();

		if (ret is null)
		{
			return base.ShouldInterceptRequest(view, request);
		}
		return ret;
	}

	public override bool ShouldOverrideUrlLoading(AndroidWebView view, IWebResourceRequest request)
	{
		return ShouldOverrideUrlLoadingCore(request) || base.ShouldOverrideUrlLoading(view, request);
	}

	private void BlazorMessageChannel(AndroidWebView webView, IVirtualBlazorWebViewProvider provider)
	{
		if (webView is null)
		{
			return;
		}

		if (provider is null)
		{
			return;
		}

		if (_webScheme is null)
		{
			return;
		}

		var nativeToJSPorts = webView.CreateWebMessageChannel();
		var nativeToJs = new BlazorWebMessageCallback(message =>
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				return;
			}

			provider.PlatformWebViewMessageReceived(_webViewCore, new WebViewMessageReceivedEventArgs
			{
				Source = _webScheme.BaseUri,
				Message = message
			});
		});

		var destPort = new[] { nativeToJSPorts[1] };
		nativeToJSPorts[0].SetWebMessageCallback(nativeToJs);

		webView.PostWebMessage(new WebMessage("capturePort", destPort), AndroidUri.Parse(_webScheme.BaseUri.AbsoluteUri)!);
	}

	private void RunBlazorStartUpScripts()
	{
		if (_webView is null)
		{
			return;
		}

		_webView.EvaluateJavascript(BlazorScriptHelper.BlazorStartedScript,
			new JavaScriptValueCallback(blazorStarted =>
			{
				var result = blazorStarted?.ToString();

				if (result != BlazorScriptHelper.UndefinedString)
				{
					return;
				}

				_webView.EvaluateJavascript(BlazorScriptHelper.BlazorMessageScript, new JavaScriptValueCallback(_ =>
				{
					_isStarted = true;
					BlazorMessageChannel(_webView, _provider!);

					_webView.EvaluateJavascript(BlazorScriptHelper.BlazorStartingScript, new JavaScriptValueCallback(_ => { }));
				}));
			})
		);
	}

	private bool ShouldOverrideUrlLoadingCore(IWebResourceRequest request)
	{
		if (_callBack is null || !Uri.TryCreate(request?.Url?.ToString(), UriKind.RelativeOrAbsolute, out var uri))
		{
			return false;
		}
		WebViewUrlLoadingEventArg args = new() { Url = uri, RawArgs = request };
		_callBack.PlatformWebViewNavigationStarting(_webViewCore, args);
		if (args.Cancel)
		{
			return false;
		}

		var newWindowEventArgs = new WebViewNewWindowEventArgs
		{
			Url = uri,
			UrlLoadingStrategy = UrlRequestStrategy.OpenInWebView
		};

		if (!_callBack.PlatformWebViewNewWindowRequest(_webViewCore, newWindowEventArgs))
		{
			return false;
		}

		var isSucceed = false;
		switch (newWindowEventArgs.UrlLoadingStrategy)
		{
			case UrlRequestStrategy.OpenExternally:
			case UrlRequestStrategy.OpenInNewWindow:
				var intent = Intent.ParseUri(uri.OriginalString, IntentUriType.Scheme);
				AndroidApplication.Context.StartActivity(intent);
				isSucceed = true;
				break;
			case UrlRequestStrategy.OpenInWebView:
				_webView?.LoadUrl(uri.OriginalString);
				isSucceed = true;
				break;
			case UrlRequestStrategy.CancelLoad:
				break;
		}

		_callBack.PlatformWebViewNavigationCompleted(_webViewCore, new WebViewUrlLoadedEventArg { IsSuccess = isSucceed });

		return true;
	}

	#endregion
}