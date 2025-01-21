#region References

using System;
using System.Drawing;
using Cornerstone.Input;
using Cornerstone.Runtime;
using Cornerstone.Windows.Inputs;
using Cornerstone.Windows.Native;

#endregion

namespace Cornerstone.Windows;

/// <summary>
/// Represents the mouse and allows for simulated input.
/// </summary>
public class WindowsMouse : Mouse
{
	#region Constants

	private const int HookMouseLowLevel = 14;

	#endregion

	#region Fields

	private NativeInput.MouseHookDelegate _monitorCallback;
	private IntPtr _monitorHandle;

	#endregion

	#region Properties

	/// <summary>
	/// Gets the current cursor for the mouse.
	/// </summary>
	public static MouseCursor Cursor => MouseCursor.Current;

	/// <inheritdoc />
	public override bool IsMonitoring => _monitorHandle != IntPtr.Zero;

	#endregion

	#region Methods

	/// <summary>
	/// Gets the current position of the mouse.
	/// </summary>
	/// <returns> The point location of the mouse cursor. </returns>
	public override Point GetCursorPosition()
	{
		return NativeInput.GetCursorPosition(out var currentMousePoint) ? currentMousePoint : new Point();
	}

	/// <summary>
	/// Simulates mouse movement to the specified location.
	/// </summary>
	/// <param name="x"> The absolute X-coordinate to move the mouse cursor to. </param>
	/// <param name="y"> The absolute Y-coordinate to move the mouse cursor to. </param>
	public override Mouse MoveTo(int x, int y)
	{
		NativeInput.SetCursorPosition(x, y);
		return this;
	}

	/// <inheritdoc />
	public override Mouse StartMonitoring()
	{
		if (IsMonitoring)
		{
			return this;
		}

		_monitorCallback = HookCallback;
		_monitorHandle = NativeInput.SetWindowsHookEx(HookMouseLowLevel, _monitorCallback, IntPtr.Zero, 0);
		return this;
	}

	/// <inheritdoc />
	public override Mouse StopMonitoring()
	{
		if (_monitorHandle != IntPtr.Zero)
		{
			NativeInput.UnhookWindowsHookEx(_monitorHandle);
			_monitorHandle = IntPtr.Zero;
		}

		return this;
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder()
	{
		return new WindowsInputBuilder();
	}

	protected override void RefreshState()
	{
		if (IsMonitoring)
		{
			// No need to manual refresh because we are monitoring.
			return;
		}

		State.LeftButton = (NativeInput.GetKeyState(0x01) & 0x8000) != 0;
		State.RightButton = (NativeInput.GetKeyState(0x02) & 0x8000) != 0;
		State.MiddleButton = (NativeInput.GetKeyState(0x04) & 0x8000) != 0;
		State.XButton1 = (NativeInput.GetKeyState(0x05) & 0x8000) != 0;
		State.XButton2 = (NativeInput.GetKeyState(0x06) & 0x8000) != 0;
		State.DateTime = DateTimeProvider.RealTime.UtcNow;
	}

	/// <inheritdoc />
	protected override InputBuilder SendInput(InputBuilder builder)
	{
		return WindowsInput.SendInput(builder);
	}

	private MouseEvent GetEvent(WindowsMessage wParam)
	{
		return wParam switch
		{
			WindowsMessage.LeftButtonDoubleClick => MouseEvent.LeftButtonDoubleClick,
			WindowsMessage.LeftButtonDown => MouseEvent.LeftButtonDown,
			WindowsMessage.LeftButtonUp => MouseEvent.LeftButtonUp,
			WindowsMessage.MiddleButtonDoubleClick => MouseEvent.MiddleButtonDoubleClick,
			WindowsMessage.MiddleButtonDown => MouseEvent.MiddleButtonDown,
			WindowsMessage.MiddleButtonUp => MouseEvent.MiddleButtonUp,
			WindowsMessage.MouseMove => MouseEvent.MouseMove,
			WindowsMessage.MouseWheel => MouseEvent.MouseWheel,
			WindowsMessage.MouseWheelHorizontal => MouseEvent.MouseWheelHorizontal,
			WindowsMessage.RightButtonDoubleClick => MouseEvent.RightButtonDoubleClick,
			WindowsMessage.RightButtonDown => MouseEvent.RightButtonDown,
			WindowsMessage.RightButtonUp => MouseEvent.RightButtonUp,
			WindowsMessage.XbuttonDoubleClick => MouseEvent.XbuttonDoubleClick,
			WindowsMessage.XbuttonDown => MouseEvent.XbuttonDown,
			WindowsMessage.XbuttonUp => MouseEvent.XbuttonUp,
			_ => MouseEvent.Unknown
		};
	}

	private int HookCallback(int code, int wParam, ref MouseInput state)
	{
		State.DateTime = DateTimeProvider.RealTime.UtcNow;
		State.Event = GetEvent((WindowsMessage) wParam);

		if (code >= 0)
		{
			State.LeftButtonDoubleClick = false;
			State.MiddleButtonDoubleClick = false;
			State.RightButtonDoubleClick = false;
			State.XButton1DoubleClick = false;
			State.XButton2DoubleClick = false;
			State.WheelHorizontalDelta = 0;
			State.WheelVerticalDelta = 0;

			switch (State.Event)
			{
				case MouseEvent.MouseMove:
				{
					State.X = state.X;
					State.Y = state.Y;
					break;
				}
				case MouseEvent.LeftButtonDoubleClick:
				{
					State.LeftButtonDoubleClick = true;
					break;
				}
				case MouseEvent.LeftButtonUp:
				{
					State.LeftButton = false;
					break;
				}
				case MouseEvent.LeftButtonDown:
				{
					State.LeftButton = true;
					break;
				}
				case MouseEvent.MiddleButtonDoubleClick:
				{
					State.MiddleButtonDoubleClick = true;
					break;
				}
				case MouseEvent.MiddleButtonUp:
				{
					State.MiddleButton = false;
					break;
				}
				case MouseEvent.MiddleButtonDown:
				{
					State.MiddleButton = true;
					break;
				}
				case MouseEvent.RightButtonDoubleClick:
				{
					State.RightButtonDoubleClick = true;
					break;
				}
				case MouseEvent.RightButtonUp:
				{
					State.RightButton = false;
					break;
				}
				case MouseEvent.RightButtonDown:
				{
					State.RightButton = true;
					break;
				}
				case MouseEvent.XbuttonDoubleClick:
				{
					if ((state.MouseData >> 16) == 1)
					{
						State.XButton1DoubleClick = true;
					}
					else
					{
						State.XButton2DoubleClick = true;
					}
					break;
				}
				case MouseEvent.XbuttonUp:
				{
					if ((state.MouseData >> 16) == 1)
					{
						State.XButton1 = false;
					}
					else
					{
						State.XButton2 = false;
					}
					break;
				}
				case MouseEvent.XbuttonDown:
				{
					if ((state.MouseData >> 16) == 1)
					{
						State.XButton1 = true;
					}
					else
					{
						State.XButton2 = true;
					}
					break;
				}
				case MouseEvent.MouseWheel:
				{
					State.WheelVerticalDelta = state.MouseData >> 16;
					break;
				}
				case MouseEvent.MouseWheelHorizontal:
				{
					State.WheelHorizontalDelta = state.MouseData >> 16;
					break;
				}
			}

			var changed = State.ShallowClone();
			OnMouseChanged(changed);

			//Debug.WriteLine(changed.ToCSharp(new CodeWriterSettings { TextFormat = TextFormat.Spaced, IgnoreDefaultValues = true, IgnoreNullValues = true }) + ",");
		}

		return NativeInput.CallNextHookEx(_monitorHandle, code, wParam, ref state);
	}

	#endregion
}