namespace Cornerstone.Keystone.Lifecycle;

/// <summary>
/// Represents an startable.
/// </summary>
public interface IStartableLifecycle
{
	#region Methods

	/// <summary>
	/// Determine if the object has been started.
	/// </summary>
	/// <returns> True if started otherwise false. </returns>
	bool IsLifecycleStarted();

	/// <summary>
	/// Start the instance.
	/// </summary>
	void StartLifecycle();

	/// <summary>
	/// Stop the instance.
	/// </summary>
	/// <remarks>
	/// Will require the instance to be restarted.
	/// </remarks>
	void StopLifecycle();

	#endregion
}