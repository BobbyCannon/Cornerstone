namespace Cornerstone.Keystone.Lifecycle;

/// <summary>
/// Represents an initializable.
/// </summary>
public interface IInitializableLifecycle
{
	#region Methods

	/// <summary>
	/// Initialize the instance.
	/// </summary>
	void InitializeLifecycle();

	/// <summary>
	/// Determine if the object has been initialized.
	/// </summary>
	/// <returns> True if initialized otherwise false. </returns>
	bool IsLifecycleInitialized();

	/// <summary>
	/// Uninitialize the instance. The instance should no longer be used
	/// until it is re-initialized.
	/// </summary>
	/// <remarks>
	/// Will require the instance to be re-initialized.
	/// </remarks>
	void UninitializeLifecycle();

	#endregion
}