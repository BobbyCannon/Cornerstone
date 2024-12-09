#region References

using System;
using System.Drawing;
using Cornerstone.Presentation.Managers;

#endregion

namespace Cornerstone.Input;

/// <summary>
/// Represents the mouse and allows for simulated and / or monitoring input.
/// </summary>
public abstract class Mouse : Manager
{
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
	/// Gets the current position of the mouse.
	/// </summary>
	/// <returns> The point location of the mouse cursor. </returns>
	public abstract Point GetCursorPosition();

	/// <summary>
	/// Simulates a mouse left-click gesture.
	/// </summary>
	public abstract Mouse LeftButtonClick();

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
	public abstract Mouse LeftButtonClick(int x, int y);

	/// <summary>
	/// Simulates a mouse middle-click gesture.
	/// </summary>
	/// <param name="x"> The absolute X-coordinate for the click. </param>
	/// <param name="y"> The absolute Y-coordinate for the click. </param>
	public abstract Mouse MiddleButtonClick(int x, int y);

	/// <summary>
	/// Simulates a mouse middle-click gesture.
	/// </summary>
	/// <param name="point"> The absolute X-coordinate and Y-coordinate for the click. </param>
	public Mouse MiddleButtonClick(Point point)
	{
		MiddleButtonClick(point.X, point.Y);
		return this;
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
	/// Simulates a mouse move to an absolute position then right button click gesture.
	/// </summary>
	/// <param name="point"> The absolute X-coordinate and Y-coordinate for the click. </param>
	public Mouse RightButtonClick(Point point)
	{
		RightButtonClick(point.X, point.Y);
		return this;
	}

	/// <summary>
	/// Simulates a mouse move to an absolute position then right button click gesture.
	/// </summary>
	/// <param name="x"> The absolute X-coordinate for the click. </param>
	/// <param name="y"> The absolute Y-coordinate for the click. </param>
	public abstract Mouse RightButtonClick(int x, int y);

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

	protected abstract InputBuilder GetInputBuilder();

	protected virtual void OnMouseChanged(MouseState e)
	{
		MouseChanged?.Invoke(this, e);
	}

	#endregion

	#region Events

	/// <summary>
	/// Called when monitoring mouse and the mouse changed.
	/// </summary>
	public event EventHandler<MouseState> MouseChanged;

	#endregion
}