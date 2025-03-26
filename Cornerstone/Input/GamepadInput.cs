#region References

using System;
using Cornerstone.Attributes;
using Cornerstone.Presentation;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Input;

public class GamepadInput : WorkerManager
{
	#region Constructors

	[DependencyInjectionConstructor]
	public GamepadInput(Gamepad gamepad, Keyboard keyboard, Mouse mouse, IDispatcher dispatcher)
		: base(1000 / 60, dispatcher)
	{
		Gamepad = gamepad;
		Keyboard = keyboard;
		Mouse = mouse;
		MouseMoveSpeed = 0.015;

		//WeakEventManager.Add<Gamepad, GamepadInput, GamepadButton, bool>(Gamepad, nameof(Gamepad.ButtonChanged), this, GamepadOnButtonChanged);
		WeakEventManager.Add<Gamepad, GamepadInput, GamepadState>(Gamepad, nameof(Gamepad.Changed), this, GamepadOnChanged);
		WeakEventManager.Add<Mouse, GamepadInput, MouseState>(Mouse, nameof(Mouse.MouseChanged), this, MouseOnMouseChanged);
	}

	#endregion

	#region Properties

	public Gamepad Gamepad { get; }

	public Keyboard Keyboard { get; }

	public Mouse Mouse { get; }

	/// <summary>
	/// Mouse move speed, in screen ratio per second (kind of).
	/// </summary>
	public double MouseMoveSpeed { get; set; }

	#endregion

	#region Methods

	protected override void Work(TimeSpan elapsed)
	{
		if (!Gamepad.IsWorking)
		{
			return;
		}

		// `MoveBy` method expects relative X and Y normal coordinates.
		// Also, we multiply by frame time to ensure the mouse speed is not affected
		// by the amount of time elapsed between each input loop iteration.

		if (Gamepad.State.RightThumbPushed)
		{
			var xD = Gamepad.State.PercentOfRightThumbX * MouseMoveSpeed * elapsed.TotalSeconds;
			var yD = -Gamepad.State.PercentOfRightThumbY * MouseMoveSpeed * elapsed.TotalSeconds;

			var x = (int) (xD * 65536);
			var y = (int) (yD * 65536);

			//Debug.WriteLine($"Right thumb has moved {Gamepad.State.RightMovementDelta.Distance} {x}, {y}");

			Mouse.MoveBy(x, y);
		}
	}

	private void GamepadOnButtonChanged(object sender, (GamepadButton button, bool pressed) e)
	{
		KeyboardKey[] keys = e.button switch
		{
			GamepadButton.DpadUp => [KeyboardKey.UpArrow],
			GamepadButton.DpadDown => [KeyboardKey.DownArrow],
			GamepadButton.DpadLeft => [KeyboardKey.LeftArrow],
			GamepadButton.DpadRight => [KeyboardKey.RightArrow],
			GamepadButton.LeftBumper => [KeyboardKey.Tab],
			GamepadButton.LeftTrigger => [KeyboardKey.LeftShift, KeyboardKey.Tab],
			_ => null
		};

		if (keys != null)
		{
			ProcessButton(e, keys);
			return;
		}

		MouseButton? button = e.button switch
		{
			GamepadButton.RightBumper => MouseButton.LeftButton,
			GamepadButton.RightTrigger => MouseButton.RightButton,
			_ => null
		};

		if (button != null)
		{
			ProcessButton(e, (MouseButton) button);
		}
	}

	private void GamepadOnChanged(object sender, GamepadState e)
	{
	}

	private void MouseOnMouseChanged(object sender, MouseState e)
	{
	}

	private void ProcessButton((GamepadButton button, bool pressed) e, MouseButton button)
	{
		switch (e.pressed)
		{
			case true when Mouse.IsButtonUp(button):
			{
				Mouse.ButtonDown(button);
				break;
			}
			case false when Mouse.IsButtonDown(button):
			{
				Mouse.ButtonUp(button);
				break;
			}
		}
	}

	private void ProcessButton((GamepadButton button, bool pressed) e, KeyboardKey[] keys)
	{
		switch (e.pressed)
		{
			case true when Keyboard.IsKeyUp(keys):
			{
				Keyboard.KeyDown(keys);
				break;
			}
			case false when Keyboard.IsKeyDown(keys):
			{
				Keyboard.KeyUp(keys);
				break;
			}
		}
	}

	#endregion
}