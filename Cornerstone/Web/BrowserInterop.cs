#region References

using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Web;

public class BrowserInteropStub : IBrowserInterop
{
	#region Methods

	public Task<string> CheckPermission(string permissionName)
	{
		return Task.FromResult(string.Empty);
	}

	public JSObject CreateElement(JSObject parent, string tagName)
	{
		return null;
	}

	public string GetLocalStorageValue(string key)
	{
		return string.Empty;
	}

	public string GetWindowLocation()
	{
		return string.Empty;
	}

	public Task<string> RequestMediaPermission(string mediaType)
	{
		return Task.FromResult(string.Empty);
	}

	public void SetLocalStorageValue(string key, string value)
	{
	}

	public string SetWindowLocation(string location)
	{
		return string.Empty;
	}

	#endregion
}

public interface IBrowserInterop
{
	#region Methods

	Task<string> CheckPermission(string permissionName);

	JSObject CreateElement(JSObject parent, string tagName);

	string GetLocalStorageValue(string key);

	string GetWindowLocation();

	Task<string> RequestMediaPermission(string mediaType);

	void SetLocalStorageValue(string key, string value);

	string SetWindowLocation(string location);

	#endregion
}