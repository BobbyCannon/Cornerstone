#region References

using System;
using System.Drawing;
using System.Threading;
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
	private const int MouseWheelClickSize = 120;

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
	/// Simulates a mouse horizontal wheel scroll gesture. Supported by Windows Vista and later.
	/// </summary>
	/// <param name="scrollAmountInClicks"> The amount to scroll in clicks. A positive value indicates that the wheel was rotated to the right; a negative value indicates that the wheel was rotated to the left. </param>
	public WindowsMouse HorizontalScroll(int scrollAmountInClicks)
	{
		var inputList = new WindowsInputBuilder().AddMouseHorizontalWheelScroll(scrollAmountInClicks * MouseWheelClickSize);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse left-click gesture.
	/// </summary>
	public override Mouse LeftButtonClick()
	{
		var inputList = new WindowsInputBuilder().AddMouseButtonClick(MouseButton.LeftButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse left-click gesture.
	/// </summary>
	/// <param name="x"> The absolute X-coordinate for the click. </param>
	/// <param name="y"> The absolute Y-coordinate for the click. </param>
	public override Mouse LeftButtonClick(int x, int y)
	{
		MoveTo(x, y);

		var inputList = new WindowsInputBuilder().AddMouseButtonClick(MouseButton.LeftButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse left button double-click gesture.
	/// </summary>
	public WindowsMouse LeftButtonDoubleClick()
	{
		var inputList = new WindowsInputBuilder().AddMouseButtonDoubleClick(MouseButton.LeftButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse left button down gesture.
	/// </summary>
	public WindowsMouse LeftButtonDown()
	{
		var inputList = new WindowsInputBuilder().AddMouseButtonDown(MouseButton.LeftButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse left button up gesture.
	/// </summary>
	public WindowsMouse LeftButtonUp()
	{
		var inputList = new WindowsInputBuilder().AddMouseButtonUp(MouseButton.LeftButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse left-click gesture.
	/// </summary>
	public WindowsMouse MiddleButtonClick()
	{
		var inputList = new WindowsInputBuilder().AddMouseButtonClick(MouseButton.MiddleButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse middle-click gesture.
	/// </summary>
	/// <param name="x"> The absolute X-coordinate for the click. </param>
	/// <param name="y"> The absolute Y-coordinate for the click. </param>
	public override Mouse MiddleButtonClick(int x, int y)
	{
		MoveTo(x, y);

		var inputList = new WindowsInputBuilder().AddMouseButtonClick(MouseButton.MiddleButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates mouse movement by the specified distance measured as a delta from the current mouse location in pixels.
	/// </summary>
	/// <param name="pixelDeltaX"> The distance in pixels to move the mouse horizontally. </param>
	/// <param name="pixelDeltaY"> The distance in pixels to move the mouse vertically. </param>
	public WindowsMouse MoveBy(int pixelDeltaX, int pixelDeltaY)
	{
		var location = GetCursorPosition();
		MoveTo(location.X + pixelDeltaX, location.Y + pixelDeltaY);
		return this;
	}

	/// <summary>
	/// Simulates mouse movement to the specified location on the primary display device.
	/// </summary>
	/// <param name="point"> The absolute X-coordinate and Y-coordinate to move the mouse to. </param>
	public Mouse MoveTo(PointF point)
	{
		return MoveTo((int) point.X, (int) point.Y);
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

	/// <summary>
	/// Simulates a mouse right button click gesture.
	/// </summary>
	public WindowsMouse RightButtonClick()
	{
		var inputList = new WindowsInputBuilder().AddMouseButtonClick(MouseButton.RightButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse move to an absolute position then right button click gesture.
	/// </summary>
	/// <param name="x"> The absolute X-coordinate for the click. </param>
	/// <param name="y"> The absolute Y-coordinate for the click. </param>
	public override Mouse RightButtonClick(int x, int y)
	{
		MoveTo(x, y);

		var inputList = new WindowsInputBuilder().AddMouseButtonClick(MouseButton.RightButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse right button double-click gesture.
	/// </summary>
	public WindowsMouse RightButtonDoubleClick()
	{
		var inputList = new WindowsInputBuilder().AddMouseButtonDoubleClick(MouseButton.RightButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse right button down gesture.
	/// </summary>
	public WindowsMouse RightButtonDown()
	{
		var inputList = new WindowsInputBuilder().AddMouseButtonDown(MouseButton.RightButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse right button up gesture.
	/// </summary>
	public WindowsMouse RightButtonUp()
	{
		var inputList = new WindowsInputBuilder().AddMouseButtonUp(MouseButton.RightButton);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Sleeps the executing thread to create a pause between simulated inputs.
	/// </summary>
	/// <param name="timeoutInMilliseconds"> The number of milliseconds to wait. </param>
	public WindowsMouse Sleep(int timeoutInMilliseconds)
	{
		Thread.Sleep(timeoutInMilliseconds);
		return this;
	}

	/// <summary>
	/// Sleeps the executing thread to create a pause between simulated inputs.
	/// </summary>
	/// <param name="timeout"> The time to wait. </param>
	public WindowsMouse Sleep(TimeSpan timeout)
	{
		Thread.Sleep(timeout);
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

	/// <summary>
	/// Simulates mouse vertical wheel scroll gesture.
	/// </summary>
	/// <param name="scrollAmountInClicks">
	/// The amount to scroll in clicks. A positive value indicates that the wheel was rotated forward, away from the user; a negative
	/// value indicates that the wheel was rotated backward, toward the user.
	/// </param>
	public WindowsMouse VerticalScroll(int scrollAmountInClicks)
	{
		var inputList = new WindowsInputBuilder().AddMouseVerticalWheelScroll(scrollAmountInClicks * MouseWheelClickSize);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse X button click gesture.
	/// </summary>
	/// <param name="buttonId"> The button id. </param>
	public WindowsMouse XButtonClick(int buttonId)
	{
		var inputList = new WindowsInputBuilder().AddMouseXButtonClick(buttonId);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse X button double-click gesture.
	/// </summary>
	/// <param name="buttonId"> The button id. </param>
	public WindowsMouse XButtonDoubleClick(int buttonId)
	{
		var inputList = new WindowsInputBuilder().AddMouseXButtonDoubleClick(buttonId);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse X button down gesture.
	/// </summary>
	/// <param name="buttonId"> The button id. </param>
	public WindowsMouse XButtonDown(int buttonId)
	{
		var inputList = new WindowsInputBuilder().AddMouseXButtonDown(buttonId);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <summary>
	/// Simulates a mouse X button up gesture.
	/// </summary>
	/// <param name="buttonId"> The button id. </param>
	public WindowsMouse XButtonUp(int buttonId)
	{
		var inputList = new WindowsInputBuilder().AddMouseXButtonUp(buttonId);
		WindowsInput.SendInput(inputList);
		return this;
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder()
	{
		return new WindowsInputBuilder();
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