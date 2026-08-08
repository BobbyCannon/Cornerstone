namespace Cornerstone.Keystone.Lifecycle;

/// <summary>
/// Represents an loadable.
/// </summary>
public interface ILoadableLifecycle
{
	#region Methods

	/// <summary>
	/// Determine if the object has been loaded.
	/// </summary>
	/// <returns> True if loaded otherwise false. </returns>
	bool IsLifecycleLoaded();

	/// <summary>
	/// Load the instance.
	/// </summary>
	void LoadLifecycle();

	/// <summary>
	/// Unload the instance.
	/// </summary>
	void UnloadLifecycle();

	#endregion
}