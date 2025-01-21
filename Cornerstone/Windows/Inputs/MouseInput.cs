#region References

using System;
using System.Runtime.InteropServices;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Windows.Inputs;

/// <summary>
/// The mouse input structure contains information about a simulated mouse event.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-msllhookstruct
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct MouseInput
{
	#region Properties

	/// <summary>
	/// Specifies the absolute position of the mouse.
	/// </summary>
	public int X;

	/// <summary>
	/// Specifies the absolute position of the mouse.
	/// </summary>
	public int Y;

	/// <summary>
	/// The data for the mouse input.
	/// </summary>
	public int MouseData;

	/// <summary>
	/// A set of bit flags that specify various aspects of mouse motion and button clicks.
	/// </summary>
	public int Flags;

	/// <summary>
	/// Time stamp for the event, in milliseconds. If this parameter is 0, the system
	/// will provide its own time stamp.
	/// </summary>
	public int Time;

	/// <summary>
	/// Specifies an additional value associated with the mouse event. An application
	/// calls GetMessageExtraInfo to obtain this extra information.
	/// </summary>
	public IntPtr ExtraInfo;

	#endregion
}