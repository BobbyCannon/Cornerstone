#region References

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// No-op keep-alive for hosts that cannot keep the CPU running in the background.
/// </summary>
public class UnsupportedKeepAlive : IKeepAlive
{
	#region Properties

	public bool IsActive => false;

	public bool IsSupported => false;

	#endregion

	#region Methods

	public void Start()
	{
	}

	public void Stop()
	{
	}

	#endregion
}
