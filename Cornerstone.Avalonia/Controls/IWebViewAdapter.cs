#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class WebViewAdapterStub : IWebViewAdapter
{
	#region Properties

	public bool CanGoBack { get; }
	public bool CanGoForward { get; }
	public string Content { get; }
	public byte[] Favicon { get; }
	public bool IsNativeSurfaceVisible { get; private set; } = true;
	public IPlatformHandle PlatformHandle { get; }
	public string Title { get; }
	public Uri Uri { get; set; }

	#endregion

	#region Methods

	public Task<WebViewSnapshot> CaptureSnapshotAsync(WebViewSnapshotOptions options = null)
	{
		return Task.FromResult(WebViewSnapshot.Failed("WebView adapter stub does not support snapshots."));
	}

	public Task ClearBrowsingDataAsync()
	{
		return Task.CompletedTask;
	}

	public void DeleteAllCookies()
	{
	}

	public void DeleteCookie(string name, string uri)
	{
	}

	public void DeleteProfile(string profileName)
	{
	}

	public IEnumerable<string> GetAvailableProfiles()
	{
		return [];
	}

	public string GetContent()
	{
		return string.Empty;
	}

	public Task<IEnumerable<WebViewCookie>> GetCookiesAsync()
	{
		return Task.FromResult((IEnumerable<WebViewCookie>) []);
	}

	public bool GoBack()
	{
		return false;
	}

	public bool GoForward()
	{
		return false;
	}

	public bool HandleKeyDown(Key key, KeyModifiers keyModifiers)
	{
		return true;
	}

	public void HandleResize(int width, int height, float zoom)
	{
	}

	public void Initialize(string profileName)
	{
	}

	public Task<string> InvokeScriptAsync(string script)
	{
		return Task.FromResult(string.Empty);
	}

	public void Navigate(Uri uri)
	{
	}

	public string NavigateToString(string text)
	{
		return string.Empty;
	}

	public void Reload()
	{
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
	}

	public void Stop()
	{
	}

	protected virtual void OnNavigationCompleted(WebViewNavigationEventArgs e)
	{
		NavigationCompleted?.Invoke(this, e);
	}

	protected virtual void OnNavigationStarted(WebViewNavigationEventArgs e)
	{
		NavigationStarted?.Invoke(this, e);
	}

	protected virtual void OnNewWindowRequested(WebViewNewWindowEventArgs e)
	{
		NewWindowRequested?.Invoke(this, e);
	}

	protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		PropertyChanged?.Invoke(this, e);
	}

	#endregion

	#region Events

	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;
	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;
	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;
	public event PropertyChangedEventHandler PropertyChanged;

	#endregion
}

public interface IWebViewAdapter : IWebView
{
	#region Properties

	/// <summary>
	/// Whether the native web surface is currently painting.
	/// When false, Avalonia content in the same slot can appear on top.
	/// </summary>
	bool IsNativeSurfaceVisible { get; }

	IPlatformHandle PlatformHandle { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Captures the currently visible web surface as a PNG for placeholder overlay mode.
	/// </summary>
	Task<WebViewSnapshot> CaptureSnapshotAsync(WebViewSnapshotOptions options = null);

	Task ClearBrowsingDataAsync();

	void DeleteAllCookies();

	void DeleteCookie(string name, string uri);

	void DeleteProfile(string profileName);

	Task<IEnumerable<WebViewCookie>> GetCookiesAsync();

	bool HandleKeyDown(Key key, KeyModifiers keyModifiers);

	void HandleResize(int width, int height, float zoom);

	void Initialize(string profileName);

	/// <summary>
	/// Shows or hides the native web surface without destroying the browser engine.
	/// Prefer this over setting IsVisible false on the NativeControlHost (which can tear it down).
	/// </summary>
	void SetNativeSurfaceVisible(bool visible);

	#endregion
}