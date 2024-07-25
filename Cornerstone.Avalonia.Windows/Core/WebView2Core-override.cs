#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Events;

#endregion

namespace Cornerstone.Avalonia.Windows.Core;

partial class WebView2Core
{
	#region Properties

	public IntPtr NativeHandler { get; private set; }

	bool IWebViewControl.IsCanGoBack => CoreWebView2?.CanGoBack ?? false;

	bool IWebViewControl.IsCanGoForward => CoreWebView2?.CanGoForward ?? false;

	bool IPlatformWebView.IsInitialized => IsInitialized;

	WebView2Core IPlatformWebView<WebView2Core>.PlatformView => this;

	object IPlatformWebView.PlatformViewContext => this;

	#endregion

	#region Methods

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				try
				{
					ClearBlazorWebViewCompleted(CoreWebView2!);
					UnregisterWebViewEvents(_coreWebView2Controller!);
					UnregisterEvents();
				}
				catch (Exception)
				{
				}

				_controllerOptions = null;
				_coreWebView2Controller = null;
				_coreWebView2Environment = null;
			}

			IsDisposed = true;
		}
	}

	void IDisposable.Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	ValueTask IAsyncDisposable.DisposeAsync()
	{
		((IDisposable) this)?.Dispose();
		return new ValueTask();
	}

	async Task<string> IWebViewControl.ExecuteScriptAsync(string javaScript)
	{
		var coreWebView2 = CoreWebView2;
		if (coreWebView2 is null)
		{
			return default;
		}

		if (string.IsNullOrEmpty(javaScript))
		{
			return default;
		}

		var result = await coreWebView2.ExecuteScriptAsync(javaScript);
		return result;
	}

	bool IWebViewControl.GoBack()
	{
		var coreWebView2 = CoreWebView2;
		if (coreWebView2 is null)
		{
			return false;
		}

		if (!coreWebView2.CanGoBack)
		{
			return false;
		}

		coreWebView2.GoBack();
		return true;
	}

	bool IWebViewControl.GoForward()
	{
		var coreWebView2 = CoreWebView2;
		if (coreWebView2 is null)
		{
			return false;
		}

		if (!coreWebView2.CanGoForward)
		{
			return false;
		}

		coreWebView2.GoForward();
		return true;
	}

	async Task<bool> IPlatformWebView.Initialize()
	{
		if (IsInitialized)
		{
			return true;
		}

		try
		{
			_callBack.PlatformWebViewCreating(this, new WebViewCreatingEventArgs());

			var environment2 = await CreateEnvironmentAsync().ConfigureAwait(true);
			_coreWebView2Environment = environment2;

			var options = CreateCoreWebView2ControllerOptions(environment2);
			var intPtr = await _hwndTaskSource.Task;

			if (options is not null)
			{
				var environment3 = environment2;
				var coreWebView2Controller = await environment3.CreateCoreWebView2ControllerAsync(intPtr, options).ConfigureAwait(true);
				_coreWebView2Controller = coreWebView2Controller;
				_controllerOptions = options;
			}
			else
			{
				var environment3 = environment2;
				var coreWebView2Controller = await environment3.CreateCoreWebView2ControllerAsync(intPtr).ConfigureAwait(true);
				_coreWebView2Controller = coreWebView2Controller;
			}

			var coreWebView2 = _coreWebView2Controller.CoreWebView2;
			if (coreWebView2 is null)
			{
				throw new ArgumentNullException(nameof(coreWebView2), "coreWebView2 is null!");
			}

			try
			{
				_browserHitTransparent = _coreWebView2Controller.IsBrowserHitTransparent;
			}
			catch (NotImplementedException)
			{
			}

			ResetWebViewSize(_coreWebView2Controller);

			if (_coreWebView2Controller.ParentWindow != intPtr)
			{
				ReparentController(_coreWebView2Controller, intPtr);
			}

			if (_coreWebView2Controller.ParentWindow != IntPtr.Zero)
			{
				SyncControllerWithParentWindow(_coreWebView2Controller);
			}

			ApplyDefaultWebViewSettings(coreWebView2);
			RegisterWebViewEvents(_coreWebView2Controller);

			if (_provider is not null)
			{
				await PrepareBlazorWebViewStarting(_provider, coreWebView2).ConfigureAwait(true);
			}

			IsInitialized = true;

			_callBack.PlatformWebViewCreated(this, new WebViewCreatedEventArgs { IsSucceed = true });
			return true;
		}
		catch (Exception ex2)
		{
			_callBack.PlatformWebViewCreated(this, new WebViewCreatedEventArgs { IsSucceed = false, Message = ex2.ToString() });
		}

		return false;
	}

	bool IWebViewControl.Navigate(Uri uri)
	{
		var coreWebView2 = CoreWebView2;
		if (coreWebView2 is null)
		{
			return false;
		}

		if (uri is null)
		{
			return false;
		}

		coreWebView2.Navigate(uri.AbsoluteUri);
		return true;
	}

	bool IWebViewControl.NavigateToString(string htmlContent)
	{
		var coreWebView2 = CoreWebView2;
		if (coreWebView2 is null)
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(htmlContent))
		{
			return false;
		}

		coreWebView2.NavigateToString(htmlContent);
		return true;
	}

	bool IWebViewControl.OpenDevToolsWindow()
	{
		var coreWebView2 = CoreWebView2;
		if (coreWebView2 is null)
		{
			return false;
		}

		coreWebView2.OpenDevToolsWindow();
		return true;
	}

	bool IWebViewControl.PostWebMessageAsJson(string webMessageAsJson, Uri baseUri)
	{
		var coreWebView2 = CoreWebView2;
		if (coreWebView2 is null)
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(webMessageAsJson))
		{
			return false;
		}

		coreWebView2.PostWebMessageAsJson(webMessageAsJson);
		return true;
	}

	bool IWebViewControl.PostWebMessageAsString(string webMessageAsString, Uri baseUri)
	{
		var coreWebView2 = CoreWebView2;
		if (coreWebView2 is null)
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(webMessageAsString))
		{
			return false;
		}

		coreWebView2.PostWebMessageAsString(webMessageAsString);
		return true;
	}

	bool IWebViewControl.Reload()
	{
		var coreWebView2 = CoreWebView2;
		if (coreWebView2 is null)
		{
			return false;
		}

		coreWebView2.Reload();
		return true;
	}

	bool IWebViewControl.Stop()
	{
		var coreWebView2 = CoreWebView2;
		if (coreWebView2 is null)
		{
			return false;
		}

		coreWebView2.Stop();
		return true;
	}

	#endregion
}