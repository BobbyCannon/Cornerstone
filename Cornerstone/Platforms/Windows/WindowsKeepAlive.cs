#region References

using System.Runtime.InteropServices;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Windows;

/// <summary>
/// Asks Windows not to sleep while keep-alive is on (SetThreadExecutionState).
/// </summary>
public class WindowsKeepAlive : IKeepAlive
{
	#region Constants

	private const uint EsContinuous = 0x80000000;
	private const uint EsSystemRequired = 0x00000001;

	#endregion

	#region Fields

	private bool _isActive;

	#endregion

	#region Constructors

	public WindowsKeepAlive()
	{
		_isActive = false;
	}

	#endregion

	#region Properties

	public bool IsActive => _isActive;

	public bool IsSupported => true;

	#endregion

	#region Methods

	public void Start()
	{
		SetThreadExecutionState(EsContinuous | EsSystemRequired);
		_isActive = true;
	}

	public void Stop()
	{
		SetThreadExecutionState(EsContinuous);
		_isActive = false;
	}

	[DllImport("kernel32.dll")]
	private static extern uint SetThreadExecutionState(uint esFlags);

	#endregion
}
