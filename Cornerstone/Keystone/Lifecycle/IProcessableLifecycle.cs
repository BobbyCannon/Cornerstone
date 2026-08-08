namespace Cornerstone.Keystone.Lifecycle;

/// <summary>
/// Represents an processable.
/// </summary>
public interface IProcessableLifecycle
{
	#region Methods

	/// <summary>
	/// Determine if the object has been processed.
	/// </summary>
	/// <returns> True if ready for processing otherwise false. </returns>
	bool CanProcessLifecycle();

	/// <summary>
	/// Process the instance.
	/// </summary>
	void ProcessLifecycle();

	#endregion
}