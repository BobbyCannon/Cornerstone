#region References

using Cornerstone.Data;

#endregion

namespace Cornerstone.Platforms.Browser;

public class BrowserInteropProxyStub : BrowserInteropProxy
{
	#region Methods

	public override string GetLocalStorageValue(string key)
	{
		return string.Empty;
	}

	public override void SetLocalStorageValue(string key, string value)
	{
	}

	protected override string GetWindowLocation()
	{
		return string.Empty;
	}

	protected override string SetWindowLocation(string location)
	{
		return string.Empty;
	}

	#endregion
}

public abstract class BrowserInteropProxy : Notifiable
{
	#region Properties

	public string WindowsLocation
	{
		get => GetWindowLocation();
		set => SetWindowLocation(value);
	}

	#endregion

	#region Methods

	public abstract string GetLocalStorageValue(string key);

	public abstract void SetLocalStorageValue(string key, string value);

	protected abstract string GetWindowLocation();

	protected abstract string SetWindowLocation(string location);

	#endregion
}