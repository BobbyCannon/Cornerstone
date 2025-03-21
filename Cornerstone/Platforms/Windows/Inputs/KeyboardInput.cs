#region References

using System;
using System.Runtime.InteropServices;

#endregion

namespace Cornerstone.Windows.Inputs;

/// <summary>
/// The keyboard input structure contains information about a simulated keyboard event.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct KeyboardInput
{
	#region Properties

	/// <summary>
	/// Specifies a virtual-key code.
	/// </summary>
	/// <remarks>
	/// https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
	/// </remarks>
	public ushort VirtualKeyCode;

	/// <summary>
	/// Specifies a hardware scan code for the key.
	/// </summary>
	public ushort ScanCode;

	/// <summary>
	/// Specifies various aspects of a keystroke.
	/// </summary>
	public uint Flags;

	/// <summary>
	/// Time stamp for the event, in milliseconds.
	/// If this parameter is zero, the system will provide its own time stamp.
	/// </summary>
	public uint Time;

	/// <summary>
	/// Specifies an additional value associated with the keystroke.
	/// Use the GetMessageExtraInfo function to obtain this information.
	/// </summary>
	public IntPtr ExtraInfo;

	#endregion
}