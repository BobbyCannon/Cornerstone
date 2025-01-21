#region References

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using Cornerstone.Presentation.Managers;

#endregion

namespace Cornerstone.Input;

/// <summary>
/// Represents the mouse and allows for simulated and / or monitoring input.
/// </summary>
public abstract class Mouse : Manager
{
	#region Constants

	private const int MouseWheelClickSize = 120;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="Mouse" />.
	/// </summary>
	protected Mouse()
	{
		State = new MouseState();
	}

	~Mouse()
	{
		StopMonitoring();
	}

	#endregion

	#region Properties

	/// <summary>
	/// True if the mouse is being monitored.
	/// </summary>
	public abstract bool IsMonitoring { get; }

	/// <summary>
	/// The last state of the mouse when monitoring.
	/// </summary>
	public MouseState State { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Calls the SendInput method to simulate mouse button down.
	/// </summary>
	/// <param name="buttons"> The buttons(s) to press down. </param>
	public Mouse ButtonDown(params MouseButton[] buttons)
	{
		if (buttons.Length <= 0)
		{
			return this;
		}

		SendInput(i => i.AddMouseButtonDown(buttons));
		RefreshState();

		return this;
	}

	/// <summary>
	/// Calls the SendInput method to simulate mouse button up.
	/// </summary>
	/// <param name="buttons"> The buttons(s) to press up. </param>
	public Mouse ButtonUp(params MouseButton[] buttons)
	{
		if (buttons.Length <= 0)
		{
			return this;
		}

		SendInput(i => i.AddMouseButtonUp(buttons));
		RefreshState();

		return this;
	}

	/// <summary>
	/// Gets the current position of the mouse.
	/// </summary>
	/// <returns> The point location of the mouse cursor. </returns>
	public abstract Point GetCursorPosition();

	/// <summary>
	/// Simulates a mouse horizontal wheel scroll gesture. Supported by Windows Vista and later.
	/// </summary>
	/// <param name="scrollAmountInClicks"> The amount to scroll in clicks. A positive value indicates that the wheel was rotated to the right; a negative value indicates that the wheel was rotated to the left. </param>
	public Mouse HorizontalScroll(int scrollAmountInClicks)
	{
		return SendInput(i => i.AddMouseHorizontalWheelScroll(scrollAmountInClicks * MouseWheelClickSize));
	}

	/// <summary>
	/// Determines whether the specified button is down.
	/// </summary>
	/// <param name="buttons"> The <see cref="MouseButton" /> for the mouse(s). </param>
	/// <returns>
	/// True if the key is down otherwise false.
	/// </returns>
	public bool IsButtonDown(params MouseButton[] buttons)
	{
		RefreshState();
		return buttons.Aggregate(false, (current, button) => current | State.IsButtonDown(button));
	}

	/// <summary>
	/// Determines whether the specified button is up.
	/// </summary>
	/// <param name="buttons"> The <see cref="MouseButton" /> for the mouse(s). </param>
	/// <returns>
	/// True if the key is up otherwise false.
	/// </returns>
	public bool IsButtonUp(params MouseButton[] buttons)
	{
		RefreshState();
		return buttons.Aggregate(false, (current, button) => current | State.IsButtonUp(button));
	}

	/// <summary>
	/// Simulates a mouse left-click gesture.
	/// </summary>
	public Mouse LeftButtonClick()
	{
		return SendInput(i => i.AddMouseButtonClick(MouseButton.LeftButton));
	}

	/// <summary>
	/// Simulates a mouse left-click gesture.
	/// </summary>
	/// <param name="point"> The absolute X-coordinate and Y-coordinate for the click. </param>
	public Mouse LeftButtonClick(Point point)
	{
		LeftButtonClick(point.X, point.Y);
		return this;
	}

	/// <summary>
	/// Simulates a mouse left-click gesture.
	/// </summary>
	/// <param name="x"> The absolute X-coordinate for the click. </param>
	/// <param name="y"> The absolute Y-coordinate for the click. </param>
	public Mouse LeftButtonClick(int x, int y)
	{
		MoveTo(x, y);
		return SendInput(i => i.AddMouseButtonClick(MouseButton.LeftButton));
	}

	/// <summary>
	/// Simulates a mouse left button double-click gesture.
	/// </summary>
	public Mouse LeftButtonDoubleClick()
	{
		return SendInput(i => i.AddMouseButtonDoubleClick(MouseButton.LeftButton));
	}

	/// <summary>
	/// Simulates a mouse left button down gesture.
	/// </summary>
	public Mouse LeftButtonDown()
	{
		return SendInput(i => i.AddMouseButtonDown(MouseButton.LeftButton));
	}

	/// <summary>
	/// Simulates a mouse left button up gesture.
	/// </summary>
	public Mouse LeftButtonUp()
	{
		return SendInput(i => i.AddMouseButtonUp(MouseButton.LeftButton));
	}

	/// <summary>
	/// Simulates a mouse middle-click gesture.
	/// </summary>
	/// <param name="x"> The absolute X-coordinate for the click. </param>
	/// <param name="y"> The absolute Y-coordinate for the click. </param>
	public Mouse MiddleButtonClick(int x, int y)
	{
		MoveTo(x, y);
		return SendInput(i => i.AddMouseButtonClick(MouseButton.MiddleButton));
	}

	/// <summary>
	/// Simulates a mouse middle-click gesture.
	/// </summary>
	/// <param name="point"> The absolute X-coordinate and Y-coordinate for the click. </param>
	public Mouse MiddleButtonClick(Point point)
	{
		return MiddleButtonClick(point.X, point.Y);
	}

	/// <summary>
	/// Simulates a mouse left-click gesture.
	/// </summary>
	public Mouse MiddleButtonClick()
	{
		return SendInput(i => i.AddMouseButtonClick(MouseButton.MiddleButton));
	}

	/// <summary>
	/// Simulates mouse movement by the specified distance measured as a delta from the current mouse location in pixels.
	/// </summary>
	/// <param name="pixelDeltaX"> The distance in pixels to move the mouse horizontally. </param>
	/// <param name="pixelDeltaY"> The distance in pixels to move the mouse vertically. </param>
	public Mouse MoveBy(int pixelDeltaX, int pixelDeltaY)
	{
		return SendInput(i => i.AddMouseMovement(pixelDeltaX, pixelDeltaY, true));
	}

	/// <summary>
	/// Simulates mouse movement to the specified location on the primary display device.
	/// </summary>
	/// <param name="point"> The absolute X-coordinate and Y-coordinate to move the mouse to. </param>
	public Mouse MoveTo(Point point)
	{
		return MoveTo(point.X, point.Y);
	}

	/// <summary>
	/// Simulates mouse movement to the specified location.
	/// </summary>
	/// <param name="x"> The absolute X-coordinate to move the mouse cursor to. </param>
	/// <param name="y"> The absolute Y-coordinate to move the mouse cursor to. </param>
	public abstract Mouse MoveTo(int x, int y);

	/// <summary>
	/// Simulates mouse movement to the specified location on the primary display device.
	/// </summary>
	/// <param name="point"> The absolute X-coordinate and Y-coordinate to move the mouse to. </param>
	public Mouse MoveTo(PointF point)
	{
		return MoveTo((int) point.X, (int) point.Y);
	}

	/// <summary>
	/// Simulates a mouse move to an absolute position then right button click gesture.
	/// </summary>
	/// <param name="point"> The absolute X-coordinate and Y-coordinate for the click. </param>
	public Mouse RightButtonClick(Point point)
	{
		return RightButtonClick(point.X, point.Y);
	}

	/// <summary>
	/// Simulates a mouse move to an absolute position then right button click gesture.
	/// </summary>
	/// <param name="x"> The absolute X-coordinate for the click. </param>
	/// <param name="y"> The absolute Y-coordinate for the click. </param>
	public Mouse RightButtonClick(int x, int y)
	{
		MoveTo(x, y);
		return SendInput(i => i.AddMouseButtonClick(MouseButton.RightButton));
	}

	/// <summary>
	/// Simulates a mouse right button click gesture.
	/// </summary>
	public Mouse RightButtonClick()
	{
		return SendInput(i => i.AddMouseButtonClick(MouseButton.RightButton));
	}

	/// <summary>
	/// Simulates a mouse right button double-click gesture.
	/// </summary>
	public Mouse RightButtonDoubleClick()
	{
		return SendInput(i => i.AddMouseButtonDoubleClick(MouseButton.RightButton));
	}

	/// <summary>
	/// Simulates a mouse right button down gesture.
	/// </summary>
	public Mouse RightButtonDown()
	{
		return SendInput(i => i.AddMouseButtonDown(MouseButton.RightButton));
	}

	/// <summary>
	/// Simulates a mouse right button up gesture.
	/// </summary>
	public Mouse RightButtonUp()
	{
		return SendInput(i => i.AddMouseButtonUp(MouseButton.RightButton));
	}

	/// <summary>
	/// Sleeps the executing thread to create a pause between simulated inputs.
	/// </summary>
	/// <param name="timeoutInMilliseconds"> The number of milliseconds to wait. </param>
	public Mouse Sleep(int timeoutInMilliseconds)
	{
		Thread.Sleep(timeoutInMilliseconds);
		return this;
	}

	/// <summary>
	/// Start monitoring the mouse input.
	/// </summary>
	/// <returns> This <see cref="Mouse" /> instance. </returns>
	public abstract Mouse StartMonitoring();

	/// <summary>
	/// Stop monitoring the mouse input.
	/// </summary>
	/// <returns> This <see cref="Mouse" /> instance. </returns>
	public abstract Mouse StopMonitoring();

	/// <summary>
	/// Simulates mouse vertical wheel scroll gesture.
	/// </summary>
	/// <param name="scrollAmountInClicks">
	/// The amount to scroll in clicks. A positive value indicates that the wheel was rotated forward, away from the user; a negative
	/// value indicates that the wheel was rotated backward, toward the user.
	/// </param>
	public Mouse VerticalScroll(int scrollAmountInClicks)
	{
		return SendInput(i => i.AddMouseVerticalWheelScroll(scrollAmountInClicks * MouseWheelClickSize));
	}

	/// <summary>
	/// Simulates a mouse X button down gesture.
	/// </summary>
	/// <param name="buttonId"> The button id. </param>
	public Mouse XButtonDown(int buttonId)
	{
		var inputList = GetInputBuilder()
			.AddMouseButtonDown(buttonId == 1
				? MouseButton.XButton1
				: MouseButton.XButton2
			);
		SendInput(inputList);
		RefreshState();
		return this;
	}

	/// <summary>
	/// Simulates a mouse X button up gesture.
	/// </summary>
	/// <param name="buttonId"> The button id. </param>
	public Mouse XButtonUp(int buttonId)
	{
		var inputList = GetInputBuilder()
			.AddMouseButtonUp(buttonId == 1
				? MouseButton.XButton1
				: MouseButton.XButton2
			);
		SendInput(inputList);
		RefreshState();
		return this;
	}

	protected abstract InputBuilder GetInputBuilder();

	protected virtual void OnMouseChanged(MouseState e)
	{
		MouseChanged?.Invoke(this, e);
	}

	protected abstract void RefreshState();

	protected Mouse SendInput(Action<InputBuilder> update)
	{
		var builder = GetInputBuilder();
		update.Invoke(builder);
		SendInput(builder);
		return this;
	}

	protected abstract InputBuilder SendInput(InputBuilder builder);

	#endregion

	#region Events

	/// <summary>
	/// Called when monitoring mouse and the mouse changed.
	/// </summary>
	public event EventHandler<MouseState> MouseChanged;

	#endregion
}