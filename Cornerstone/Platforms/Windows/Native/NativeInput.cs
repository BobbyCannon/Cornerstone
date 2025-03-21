#region References

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Cornerstone.Windows.Inputs;

#endregion

namespace Cornerstone.Windows.Native;

internal class NativeInput
{
	#region Methods

	[DllImport("User32.dll")]
	public static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref KeyboardInput lParam);

	[DllImport("User32.dll")]
	public static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref MouseInput lParam);

	[DllImport("user32.dll", EntryPoint = "GetCursorPos", SetLastError = true)]
	public static extern bool GetCursorPosition(out Point lpMousePoint);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern short GetKeyState(ushort virtualKeyCode);

	[DllImport("user32.dll")]
	public static extern IntPtr GetMessageExtraInfo();

	[DllImport("user32.dll")]
	public static extern uint MapVirtualKey(uint uCode, uint uMapType);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern uint SendInput(uint numberOfInputs, InputTypeWithData[] inputs, int sizeOfInputStructure);

	[DllImport("user32.dll", SetLastError = true, EntryPoint = "SetCursorPos")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SetCursorPosition(int x, int y);

	/// <summary>
	/// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexa
	/// </summary>
	[DllImport("User32.dll")]
	public static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookDelegate callback, IntPtr hInstance, uint threadId);

	/// <summary>
	/// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexa
	/// </summary>
	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern IntPtr SetWindowsHookEx(int idHook, MouseHookDelegate callback, IntPtr hInstance, uint threadId);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll")]
	public static extern short VkKeyScan(char ch);

	#endregion

	#region Delegates

	public delegate int KeyboardHookDelegate(int code, int wParam, ref KeyboardInput lParam);

	public delegate int MouseHookDelegate(int code, int wParam, ref MouseInput lParam);

	#endregion
}