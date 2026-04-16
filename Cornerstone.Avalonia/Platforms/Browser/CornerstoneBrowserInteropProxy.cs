#region References

using Cornerstone.Platforms.Browser;

#endregion

namespace Cornerstone.Avalonia.Platforms.Browser;

public class CornerstoneBrowserInteropProxy : BrowserInteropProxy
{
	#region Methods

	/// <inheritdoc />
	public override string GetLocalStorageValue(string key)
	{
		return BrowserInterop.GetLocalStorageValue(key);
	}

	/// <inheritdoc />
	public override void SetLocalStorageValue(string key, string value)
	{
		BrowserInterop.SetLocalStorageValue(key, value);
	}

	/// <inheritdoc />
	protected override string GetWindowLocation()
	{
		return BrowserInterop.GetWindowLocation();
	}

	/// <inheritdoc />
	protected override string SetWindowLocation(string location)
	{
		return BrowserInterop.SetWindowLocation(location);
	}

	#endregion
}