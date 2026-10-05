namespace Cornerstone.Windows.Inputs;

/// <summary>
/// Notice: Only windows that have the CS_DBLCLKS style can receive WM_LBUTTONDBLCLK messages...
/// https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-lbuttondblclk?redirectedfrom=MSDN
/// </summary>
internal enum WindowsMessage : uint
{
	MouseMove = 0x200,
	LeftButtonDown = 0x201,
	LeftButtonUp = 0x202,
	LeftButtonDoubleClick = 0x203,
	RightButtonDown = 0x204,
	RightButtonUp = 0x205,
	RightButtonDoubleClick = 0x206,
	MiddleButtonDown = 0x207,
	MiddleButtonUp = 0x208,
	MiddleButtonDoubleClick = 0x209,
	MouseWheel = 0x20A,
	XbuttonDown = 0x20B,
	XbuttonUp = 0x20C,
	XbuttonDoubleClick = 0x20D,
	MouseWheelHorizontal = 0x20E,
	KeyDown = 0x100,
	KeyUp = 0x101,
	SystemKeyDown = 0x104,
	SystemKeyUp = 0x105
}