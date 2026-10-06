#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Presentation.Controls.Web;

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

public interface IWebViewAdapter : IWebView, INativeSurface
{
	#region Methods

	/// <summary>
	/// Bind the adapter to the host control. Linux reparents / injects input; browser attaches the DOM overlay.
	/// Other platforms return <see cref="INativeSurface.PlatformHandle" />.
	/// </summary>
	IPlatformHandle AttachToHost(IPlatformHandle parent, InputElement inputHost)
	{
		return PlatformHandle;
	}

	Task ClearBrowsingDataAsync();

	void DeleteAllCookies();

	void DeleteCookie(string name, string uri);

	void DeleteProfile(string profileName);

	/// <summary>
	/// Undo <see cref="AttachToHost" /> (overlay detach, Linux input unhook). No-op on HWND/view adapters.
	/// </summary>
	void DetachFromHost()
	{
	}

	Task<IEnumerable<WebViewCookie>> GetCookiesAsync();

	bool HandleKeyDown(Key key, KeyModifiers keyModifiers);

	void Initialize(string profileName);

	#endregion
}