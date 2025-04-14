#region References

using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Avalonia.Platforms.Browser;

public partial class BrowserInteropProxy
{
	#region Methods

	[JSImport("window.checkPermission", "Cornerstone.Browser.js")]
	public static partial Task<string> CheckPermission(string permissionName);

	[JSImport("document.createElement", "Cornerstone.Browser.js")]
	public static partial JSObject CreateElement(JSObject parent, string tagName);

	[JSImport("localStorage.getValue", "Cornerstone.Browser.js")]
	public static partial string GetLocalStorageValue(string key);

	[JSImport("window.getLocation", "Cornerstone.Browser.js")]
	public static partial string GetWindowLocation();

	[JSImport("document.hideElement", "Cornerstone.Browser.js")]
	public static partial JSObject HideElement(JSObject parent);

	[JSImport("window.requestMediaPermission", "Cornerstone.Browser.js")]
	public static partial Task<string> RequestMediaPermission(string mediaType);

	[JSImport("localStorage.setValue", "Cornerstone.Browser.js")]
	public static partial void SetLocalStorageValue(string key, string value);

	[JSImport("window.setLocation", "Cornerstone.Browser.js")]
	public static partial string SetWindowLocation(string location);

	[JSImport("document.showElement", "Cornerstone.Browser.js")]
	public static partial JSObject ShowElement(JSObject parent);

	#endregion
}