#region References

using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Avalonia.Platforms.Browser;

public class BrowserInterop : IBrowserInterop
{
	#region Methods

	public Task<string> CheckPermission(string permissionName)
	{
		//return Task.FromResult(string.Empty);
		return BrowserInteropProxy.CheckPermission(permissionName);
	}

	public JSObject CreateElement(JSObject parent, string tagName)
	{
		return BrowserInteropProxy.CreateElement(parent, tagName);
	}

	public string GetLocalStorageValue(string key)
	{
		return BrowserInteropProxy.GetLocalStorageValue(key);
	}

	public string GetWindowLocation()
	{
		return BrowserInteropProxy.GetWindowLocation();
	}

	public Task<string> RequestMediaPermission(string mediaType)
	{
		return Task.FromResult(string.Empty);
		//return BrowserInteropProxy.RequestMediaPermission(mediaType);
	}

	public void SetLocalStorageValue(string key, string value)
	{
		BrowserInteropProxy.SetLocalStorageValue(key, value);
	}

	public string SetWindowLocation(string location)
	{
		return BrowserInteropProxy.SetWindowLocation(location);
	}

	#endregion
}