#region References

using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform;

#endregion

namespace Cornerstone.Avalonia.Controls;

public interface IWebViewAdapter : IWebView
{
	#region Properties

	IPlatformHandle PlatformHandle { get; }

	#endregion

	#region Methods

	Task ClearBrowsingDataAsync();

	void DeleteAllCookies();

	void DeleteCookie(string name, string uri);

	void DeleteProfile(string profileName);

	Task<IEnumerable<WebViewCookie>> GetCookiesAsync();

	bool HandleKeyDown(Key key, KeyModifiers keyModifiers);

	void HandleResize(int width, int height, float zoom);

	void Initialize(string profileName);

	#endregion
}