#region References

using System;
using System.Runtime.Versioning;
using Android.Runtime;
using Android.Webkit;
using Cornerstone.Avalonia.Controls;
using Bitmap = Android.Graphics.Bitmap;

#endregion

namespace Cornerstone.Avalonia.Platforms.Android.Clients;

[SupportedOSPlatform("android23.0")]
internal class CornerstoneWebViewClient : WebViewClient
{
	#region Fields

	private readonly WebViewAdapter _webViewAdapter;

	#endregion

	#region Constructors

	public CornerstoneWebViewClient(WebViewAdapter webViewAdapter)
	{
		ArgumentNullException.ThrowIfNull(webViewAdapter);
		_webViewAdapter = webViewAdapter;
	}

	protected CornerstoneWebViewClient(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
	{
		// This constructor is called whenever the .NET proxy was disposed, and it was recreated by Java. It also
		// happens when overridden methods are called between execution of this constructor and the one above.
		// because of these facts, we have to check all methods below for null field references and properties.
	}

	#endregion

	#region Methods

	public override void OnPageFinished(AndroidWebView view, string url)
	{
		_webViewAdapter.OnNavigationCompleted(new WebViewNavigationEventArgs { Uri = new Uri(url) });
		base.OnPageFinished(view, url);
	}

	/// <inheritdoc />
	public override void OnPageStarted(AndroidWebView view, string url, Bitmap favicon)
	{
		_webViewAdapter.OnNavigationStarted(new WebViewNavigationEventArgs { Uri = new Uri(url) });
		base.OnPageStarted(view, url, favicon);
	}

	#endregion
}