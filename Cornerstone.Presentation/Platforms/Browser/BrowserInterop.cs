#region References

using System.Runtime.InteropServices.JavaScript;
using Cornerstone.Presentation.Browser.Interop;

#endregion

namespace Cornerstone.Presentation.Platforms.Browser;

public partial class BrowserInterop
{
	#region Methods

	[JSImport("globalThis.document.createElement")]
	public static partial JSObject CreateElement(string tagName);

	[JSImport("document.createElement", CornerstoneModule.MainModuleName)]
	public static partial JSObject CreateElement(JSObject parent, string tagName);

	[JSImport("document.attachOverlay", CornerstoneModule.MainModuleName)]
	public static partial JSObject AttachOverlay(JSObject element);

	[JSImport("document.setOverlayBounds", CornerstoneModule.MainModuleName)]
	public static partial JSObject SetOverlayBounds(JSObject element, double x, double y, double width, double height);

	[JSImport("document.detachOverlay", CornerstoneModule.MainModuleName)]
	public static partial JSObject DetachOverlay(JSObject element);

	[JSImport("localStorage.getValue", CornerstoneModule.MainModuleName)]
	public static partial string GetLocalStorageValue(string key);

	[JSImport("window.getLocation", CornerstoneModule.MainModuleName)]
	public static partial string GetWindowLocation();

	[JSImport("document.hideElement", CornerstoneModule.MainModuleName)]
	public static partial JSObject HideElement(JSObject parent);

	[JSImport("localStorage.setValue", CornerstoneModule.MainModuleName)]
	public static partial void SetLocalStorageValue(string key, string value);

	[JSImport("window.setLocation", CornerstoneModule.MainModuleName)]
	public static partial string SetWindowLocation(string location);

	[JSImport("document.showElement", CornerstoneModule.MainModuleName)]
	public static partial JSObject ShowElement(JSObject parent);

	#endregion
}
