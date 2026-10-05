#region References

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Asks the OS to keep the process runnable (Android foreground service, desktop sleep inhibit).
/// </summary>
public interface IKeepAlive
{
	#region Properties

	bool IsActive { get; }

	bool IsSupported { get; }

	#endregion

	#region Methods

	void Start();

	void Stop();

	#endregion
}
