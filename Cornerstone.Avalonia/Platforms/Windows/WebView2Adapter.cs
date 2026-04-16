#region References

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Input;
using Avalonia.Platform;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

#endregion

namespace Cornerstone.Avalonia.Platforms.Windows;

[SupportedOSPlatform("Windows")]
internal partial class WebView2Adapter : Notifiable, IWebViewAdapter, IDisposable
{
	#region Constants

	public const string ProfilePrefix = "WV2Profile_";
	private const uint SwpNoZOrder = 0x0004;

	#endregion

	#region Fields

	private readonly Color _defaultBackground;
	private readonly IRuntimeInformation _runtimeInformation;
	private readonly WebView2 _webView;
	private bool _webViewInitialized;
	private Color _originalBackground;

	#endregion

	#region Constructors

	public WebView2Adapter(IRuntimeInformation runtimeInformation)
	{
		_runtimeInformation = runtimeInformation;
		_webView = new WebView2();
		_defaultBackground = ResourceService.GetColor("Background03").ToDrawingColor();

		PlatformHandle = new PlatformHandle(_webView.Handle, "HWND");
	}

	#endregion

	#region Properties

	public bool CanGoBack => _webView.CanGoBack;

	public bool CanGoForward => _webView.CanGoForward;

	[Notify]
	public partial string Content { get; set; }

	[Notify]
	public partial byte[] Favicon { get; private set; }

	public IPlatformHandle PlatformHandle { get; }

	[Notify]
	public partial string Title { get; private set; }

	public Uri Uri
	{
		get => _webView.Source;
		set
		{
			_webView.Source = value;
			OnPropertyChanged();
		}
	}

	#endregion

	#region Methods

	public async Task ClearBrowsingDataAsync()
	{
		if (_webViewInitialized)
		{
			await _webView.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.AllProfile);
		}
	}

	public void DeleteAllCookies()
	{
		if (_webViewInitialized)
		{
			_webView.CoreWebView2.Profile.CookieManager.DeleteAllCookies();
		}
	}

	public void DeleteCookie(string name, string uri)
	{
		if (!_webViewInitialized)
		{
			return;
		}

		_webView.CoreWebView2.Profile.CookieManager.DeleteCookies(name, uri);
	}

	public void DeleteProfile(string profileName)
	{
		var folder = Path.Join(GetUserDataFolder(), "EBWebView", ProfilePrefix + profileName.ToLower());
		var folderInfo = new DirectoryInfo(folder);
		if (folderInfo.Exists)
		{
			folderInfo.SafeDelete();
		}
	}

	public void Dispose()
	{
		var webView = _webView;

		if (webView.CoreWebView2 != null)
		{
			webView.CoreWebView2.ContextMenuRequested -= CoreWebView2OnContextMenuRequested;
			webView.CoreWebView2.DocumentTitleChanged -= OnCoreWebView2OnDocumentTitleChanged;
			webView.CoreWebView2.FaviconChanged -= OnCoreWebView2OnFaviconChanged;
			webView.CoreWebView2.FrameNavigationCompleted -= CoreWebView2OnFrameNavigationCompleted;
			webView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
			webView.CoreWebView2.PermissionRequested -= OnPermissionRequested;
			webView.CoreWebView2.WebResourceRequested -= OnCoreWebView2OnWebResourceRequested;
		}

		webView.CoreWebView2InitializationCompleted -= OnCoreWebView2InitializationCompleted;
		webView.NavigationStarting -= OnWebViewOnNavigationStarting;
		webView.NavigationCompleted -= OnWebViewOnNavigationCompleted;
		webView.SourceChanged -= OnWebViewOnSourceChanged;
		webView.Dispose();
	}

	public IEnumerable<string> GetAvailableProfiles()
	{
		try
		{
			var rootFolder = Path.Combine(GetUserDataFolder(), "EBWebView");
			if (!Directory.Exists(rootFolder))
			{
				return [];
			}

			var profiles = Directory
				.GetDirectories(rootFolder)
				.Select(Path.GetFileName)
				.Where(x => (x != null) && x.StartsWith(ProfilePrefix, StringComparison.OrdinalIgnoreCase))
				.Select(x => x[ProfilePrefix.Length..])
				.Where(x => !string.IsNullOrEmpty(x) && (x.Length >= 2))
				.Select(x => char.ToUpper(x[0]) + x[1..])
				.ToList();

			return profiles;
		}
		catch
		{
			return [];
		}
	}

	public string GetContent()
	{
		return Content = InvokeScriptAsync("document.documentElement.outerHTML;").Result;
	}

	public async Task<IEnumerable<WebViewCookie>> GetCookiesAsync()
	{
		var cookieManager = _webView.CoreWebView2.CookieManager;
		var cookies = await cookieManager.GetCookiesAsync(Uri.AbsoluteUri);

		return cookies
			.Select(c => new WebViewCookie
			{
				Name = c.Name,
				Domain = c.Domain,
				Expires = c.Expires,
				IsHttpOnly = c.IsHttpOnly,
				IsHostOnly = (c.Domain?.Length >= 1) && (c.Domain[0] != '.'),
				IsSecure = c.IsSecure,
				IsSession = c.IsSession,
				Path = c.Path,
				SameSite = (WebViewCookieSameSite) c.SameSite,
				Value = c.Value
			});
	}

	public bool GoBack()
	{
		_webView.GoBack();
		return true;
	}

	public bool GoForward()
	{
		_webView.GoForward();
		return true;
	}

	public bool HandleKeyDown(Key key, KeyModifiers keyModifiers)
	{
		return false;
	}

	public void HandleResize(int width, int height, float zoom)
	{
		SetWindowPos(_webView.Handle, IntPtr.Zero, 0, 0, width, height, SwpNoZOrder);
	}

	public void Initialize(string profileName)
	{
		if (string.IsNullOrWhiteSpace(profileName))
		{
			profileName = WebView.DefaultProfileName;
		}

		_webView.CreationProperties = new CoreWebView2CreationProperties
		{
			UserDataFolder = GetUserDataFolder(),
			ProfileName = profileName
		};

		var options = new CoreWebView2EnvironmentOptions("--disable-features=WebAuthenticationUseNativeWinApi")
		{
			AreBrowserExtensionsEnabled = false,
			ExclusiveUserDataFolderAccess = true
		};

		var environment = CoreWebView2Environment.CreateAsync(null, GetUserDataFolder(), options).GetAwaiter().GetResult();

		_webView.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
		_webView.NavigationStarting += OnWebViewOnNavigationStarting;
		_webView.NavigationCompleted += OnWebViewOnNavigationCompleted;
		_webView.SourceChanged += OnWebViewOnSourceChanged;
		_originalBackground = _webView.DefaultBackgroundColor;
		_webView.EnsureCoreWebView2Async(environment);
	}

	public Task<string> InvokeScriptAsync(string script)
	{
		return _webView.ExecuteScriptAsync(script);
	}

	public void Navigate(Uri uri)
	{
		if (Uri?.OriginalString == uri?.OriginalString)
		{
			return;
		}

		Uri = uri;
	}

	public string NavigateToString(string text)
	{
		if (_webViewInitialized)
		{
			_webView.NavigateToString(text);
		}
		Content = text;
		return text;
	}

	public void Reload()
	{
		_webView.CoreWebView2.Reload();
	}

	[DllImport("user32.dll")]
	public static extern bool SetParent(IntPtr hWnd, IntPtr hWndNewParent);

	public void Stop()
	{
		_webView.Stop();
	}

	protected virtual void OnNavigationCompleted()
	{
		OnPropertyChanged(nameof(CanGoBack));
		OnPropertyChanged(nameof(CanGoForward));
		OnPropertyChanged(nameof(Uri));

		Title = _webView.CoreWebView2.DocumentTitle;
		NavigationCompleted?.Invoke(this, new WebViewNavigationEventArgs { Uri = _webView.Source });
	}

	protected virtual void OnNavigationStarted(string uri)
	{
		NavigationStarted?.Invoke(this, new WebViewNavigationEventArgs { Uri = new Uri(uri) });
	}

	private void CoreWebView2OnContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
	{
		if (!e.ContextMenuTarget.HasSelection)
		{
			return;
		}

		var environment = _webView.CoreWebView2.Environment;
		var title = $"Search the web for \"{e.ContextMenuTarget.SelectionText}\".";
		var item = environment.CreateContextMenuItem(title, null, CoreWebView2ContextMenuItemKind.Command);
		item.CustomItemSelected += (_, _) => { OnNewWindowRequested($"https://www.bing.com/search?q={HttpUtility.UrlEncode(e.ContextMenuTarget.SelectionText)}"); };
		e.MenuItems.Insert(0, item);
	}

	private void CoreWebView2OnFrameNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
	{
		OnNavigationCompleted();
	}

	private string GetUserDataFolder()
	{
		return Path.Join(_runtimeInformation.ApplicationDataLocation, "Browser");
	}

	private void OnCoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
	{
		CornerstoneApplication.CornerstoneDispatcher.Dispatch(() =>
			{
				var webView = (WebView2) sender;

				//webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
				webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
				webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
				webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
				webView.CoreWebView2.Settings.IsScriptEnabled = true;
				webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

				_webViewInitialized = webView == _webView;

				webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
				webView.CoreWebView2.Profile.IsGeneralAutofillEnabled = false;
				webView.CoreWebView2.Profile.IsPasswordAutosaveEnabled = false;

				webView.CoreWebView2.ContextMenuRequested += CoreWebView2OnContextMenuRequested;
				webView.CoreWebView2.DocumentTitleChanged += OnCoreWebView2OnDocumentTitleChanged;
				webView.CoreWebView2.FaviconChanged += OnCoreWebView2OnFaviconChanged;
				webView.CoreWebView2.FrameNavigationCompleted += CoreWebView2OnFrameNavigationCompleted;
				webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
				webView.CoreWebView2.PermissionRequested += OnPermissionRequested;

				webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
				webView.CoreWebView2.WebResourceRequested += OnCoreWebView2OnWebResourceRequested;

				if (_webViewInitialized
					&& !string.IsNullOrWhiteSpace(Content))
				{
					NavigateToString(Content);
				}
			}
		);
	}

	private void OnCoreWebView2OnDocumentTitleChanged(object sender, object e)
	{
		Title = _webView.CoreWebView2.DocumentTitle;
	}

	private void OnCoreWebView2OnFaviconChanged(object sender, object e)
	{
		_ = RefreshFaviconAsync();
	}

	private void OnCoreWebView2OnWebResourceRequested(object s, CoreWebView2WebResourceRequestedEventArgs args)
	{
		var url = args.Request.Uri.ToLower();
		if (AdBlocking.ShouldBlock(url))
		{
			args.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(null, 200, "OK", "");
		}
	}

	private void OnNewWindowRequested(object s, CoreWebView2NewWindowRequestedEventArgs args)
	{
		args.Handled = OnNewWindowRequested(args.Uri);
	}

	private bool OnNewWindowRequested(string uri)
	{
		var handler = NewWindowRequested;
		if (handler == null)
		{
			return false;
		}

		var newArgs = new WebViewNewWindowEventArgs { Uri = new Uri(uri) };
		handler.Invoke(this, newArgs);
		return newArgs.Handled;
	}

	private void OnPermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
	{
		if (e.PermissionKind == CoreWebView2PermissionKind.FileReadWrite)
		{
			e.State = CoreWebView2PermissionState.Allow;
		}
	}

	private void OnWebViewOnNavigationCompleted(object s, CoreWebView2NavigationCompletedEventArgs args)
	{
		if (_webView != null)
		{
			_webView.DefaultBackgroundColor = _originalBackground;
		}
		OnNavigationCompleted();
	}

	private void OnWebViewOnNavigationStarting(object s, CoreWebView2NavigationStartingEventArgs args)
	{
		if (_webView != null)
		{
			_webView.DefaultBackgroundColor = _defaultBackground;
		}
		OnNavigationStarted(args.Uri);
	}

	private void OnWebViewOnSourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
	{
		OnPropertyChanged(nameof(Uri));
	}

	private async Task RefreshFaviconAsync()
	{
		await using var stream = await _webView.CoreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
		Favicon = stream.ReadByteArray();
		OnPropertyChanged(nameof(Favicon));
	}

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

	#endregion

	#region Events

	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;
	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;
	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;

	#endregion
}