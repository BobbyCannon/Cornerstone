#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Webkit;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Cornerstone.Avalonia.Android.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Extensions;

#endregion

namespace Cornerstone.Avalonia.Android.Clients;

internal class AvaloniaWebChromeClient : WebChromeClient
{
	#region Fields

	private readonly AndroidWebViewCore _androidWebViewCore;
	private readonly TopLevel _topLevel;

	#endregion

	#region Constructors

	public AvaloniaWebChromeClient(AndroidWebViewCore androidWebViewCore)
	{
		_androidWebViewCore = androidWebViewCore;
		var topLevel = androidWebViewCore.GetTopLevel();
		if (topLevel is null)
		{
			throw new ArgumentNullException(nameof(topLevel));
		}

		_topLevel = topLevel;
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

	public override bool OnShowFileChooser(AndroidWebView webView, IValueCallback filePathCallback, FileChooserParams fileChooserParams)
	{
		if (filePathCallback is null)
		{
			return base.OnShowFileChooser(webView, filePathCallback, fileChooserParams);
		}

		CallFilePickerAsync(filePathCallback, fileChooserParams).FireAndForget();
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

		var fileUris = new List<AndroidUri>(fileResults.Count());
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
					AppleUniformTypeIdentifiers = new string[1] { "public.accepted" },
					MimeTypes = new string[1] { "accepted/*" }
				}
			}
		};
		return pickOptions;
	}

	#endregion
}