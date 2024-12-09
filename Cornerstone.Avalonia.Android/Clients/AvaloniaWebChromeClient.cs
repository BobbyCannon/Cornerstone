#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Service.QuickSettings;
using Android.Webkit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Cornerstone.Avalonia.Extensions;
using Debug = System.Diagnostics.Debug;

#endregion

namespace Cornerstone.Avalonia.Android.Clients;

internal class CornerstoneWebChromeClient : WebChromeClient
{
	#region Fields

	private readonly WebViewAdapter _androidWebViewCore;
	private readonly TopLevel _topLevel;

	#endregion

	#region Constructors

	public CornerstoneWebChromeClient(WebViewAdapter androidWebViewCore)
	{
		_androidWebViewCore = androidWebViewCore;

		var topLevel = Application.Current.GetTopLevel();
		_topLevel = topLevel ?? throw new ArgumentNullException(nameof(topLevel));
	}

	#endregion

	#region Methods

	public override bool OnCreateWindow(AndroidWebView view, bool isDialog, bool isUserGesture, Message resultMsg)
	{
		if (view?.Context is not null)
		{
			var requestUrl = view.GetHitTestResult().Extra;
			var intent = new Intent(Intent.ActionView, AndroidUri.Parse(requestUrl));
			intent.SetFlags(ActivityFlags.NewTask);
			view.Context.StartActivity(intent);
		}

		return false;
	}

	/// <inheritdoc />
	public override void OnReceivedTitle(WebView view, string title)
	{
		_androidWebViewCore.Title = title;
		base.OnReceivedTitle(view, title);
	}

	/// <inheritdoc />
	public override void OnReceivedIcon(WebView view, Bitmap icon)
	{
		_androidWebViewCore.Favicon = ToByteArray(icon);
		base.OnReceivedIcon(view, icon);
	}

	private byte[] ToByteArray(Bitmap b)
	{
		if (b == null)
		{
			return null;
		}

		using var stream = new MemoryStream();
		b.Compress(Bitmap.CompressFormat.Png, 0, stream);
		return stream.ToArray();
	}

	/// <inheritdoc />
	public override bool OnJsAlert(WebView view, string url, string message, JsResult result)
	{
		Debug.WriteLine(message);
		return base.OnJsAlert(view, url, message, result);
	}

	public override bool OnShowFileChooser(AndroidWebView webView, IValueCallback filePathCallback, FileChooserParams fileChooserParams)
	{
		if (filePathCallback is null)
		{
			return base.OnShowFileChooser(webView, null, fileChooserParams);
		}

		_ = CallFilePickerAsync(filePathCallback, fileChooserParams);
		return true;
	}

	private async Task CallFilePickerAsync(IValueCallback filePathCallback, FileChooserParams fileChooserParams)
	{
		var pickOptions = GetPickOptions(fileChooserParams);
		if (pickOptions is null)
		{
			filePathCallback.OnReceiveValue(default);
			return;
		}

		var fileResults = await _topLevel.StorageProvider.OpenFilePickerAsync(pickOptions);
		if (fileResults?.All(f => f is null) ?? true)
		{
			filePathCallback.OnReceiveValue(default);
			return;
		}

		var fileUris = new List<AndroidUri>(fileResults.Count);
		foreach (var fileResult in fileResults)
		{
			if (fileResult is null)
			{
				continue;
			}

			var javaFile = new JavaFile(fileResult.Path.AbsoluteUri);
			var androidUri = AndroidUri.FromFile(javaFile);

			if (androidUri is not null)
			{
				fileUris.Add(androidUri);
			}
		}

		filePathCallback.OnReceiveValue(fileUris.ToArray());
	}

	private static FilePickerOpenOptions GetPickOptions(FileChooserParams fileChooserParams)
	{
		if (fileChooserParams is null)
		{
			return default;
		}

		var acceptedFileTypes = fileChooserParams.GetAcceptTypes();
		if (acceptedFileTypes is null || ((acceptedFileTypes.Length == 1) && string.IsNullOrEmpty(acceptedFileTypes[0])))
		{
			return null;
		}

		var allowMultiple = fileChooserParams.Mode == ChromeFileChooserMode.OpenMultiple;

		var pickOptions = new FilePickerOpenOptions
		{
			AllowMultiple = allowMultiple,
			FileTypeFilter = new List<FilePickerFileType>
			{
				new("Accepted File")
				{
					Patterns = acceptedFileTypes,
					AppleUniformTypeIdentifiers = ["public.accepted"],
					MimeTypes = ["accepted/*"]
				}
			}
		};
		return pickOptions;
	}

	#endregion
}