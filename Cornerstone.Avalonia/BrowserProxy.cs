namespace Cornerstone.Avalonia;

public class BrowserProxy : IBrowserProxy
{
	#region Methods

	/// <inheritdoc />
	public virtual string GetLocalStorageValue(string key)
	{
		return string.Empty;
	}

	/// <inheritdoc />
	public virtual string GetWindowLocation()
	{
		return string.Empty;
	}

	/// <inheritdoc />
	public virtual void SetLocalStorageValue(string key, string value)
	{
	}

	/// <inheritdoc />
	public virtual string SetWindowLocation(string location)
	{
		return string.Empty;
	}

	#endregion
}

public interface IBrowserProxy
{
	#region Methods

	string GetLocalStorageValue(string key);

	string GetWindowLocation();

	void SetLocalStorageValue(string key, string value);

	string SetWindowLocation(string location);

	#endregion
}