#region References

using System;
using System.Collections.Generic;
using Cornerstone.Extensions;
using Cornerstone.Input;
using Cornerstone.Windows.Inputs;
using Cornerstone.Windows.Native;

#endregion

namespace Cornerstone.Windows;

/// <summary>
/// A helper class for building a list of <see cref="InputTypeWithData" /> messages ready to be sent to the native Windows API.
/// </summary>
public class WindowsInputBuilder : InputBuilder
{
	#region Fields

	/// <summary>
	/// The public list of <see cref="InputTypeWithData" /> messages being built by this instance.
	/// </summary>
	private readonly List<InputTypeWithData> _inputList;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="InputBuilder" /> class.
	/// </summary>
	/// <param name="textInputAsKeyPresses"> Defaults the keyboard SendInput to send text as key presses if true. </param>
	public WindowsInputBuilder(bool textInputAsKeyPresses = true) : base(textInputAsKeyPresses)
	{
		_inputList = [];

		TextInputAsKeyPresses = textInputAsKeyPresses;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="InputBuilder" /> class.
	/// </summary>
	/// <param name="text"> The text to add. </param>
	/// <param name="textInputAsKeyPresses"> Defaults the keyboard SendInput to send text as key presses if true. </param>
	public WindowsInputBuilder(string text, bool textInputAsKeyPresses) : this(textInputAsKeyPresses)
	{
		Add(text);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="InputBuilder" /> class.
	/// </summary>
	/// <param name="strokes"> The stroke(s) to add. </param>
	public WindowsInputBuilder(params KeyStroke[] strokes) : this()
	{
		Add(strokes);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="InputBuilder" /> class.
	/// </summary>
	/// <param name="keys"> The keys to press. </param>
	public WindowsInputBuilder(params KeyboardKey[] keys) : this()
	{
		AddKeyPress(keys);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="InputBuilder" /> class.
	/// </summary>
	/// <param name="modifiers"> The modifier key(s). </param>
	/// <param name="keys"> The list of keys to press. </param>
	public WindowsInputBuilder(KeyboardModifier modifiers, params KeyboardKey[] keys) : this()
	{
		AddKeyPress(modifiers, keys);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Adds the character to the builder.
	/// </summary>
	/// <param name="character"> The character to be added an input. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public override InputBuilder Add(char character)
	{
		if (TextInputAsKeyPresses)
		{
			AddKeyPress(FromCharacter(character));
			return this;
		}

		var input = new InputTypeWithData
		{
			Type = (uint) InputType.Keyboard,
			Data =
			{
				Keyboard =
					new KeyboardInput
					{
						VirtualKeyCode = 0,
						ScanCode = character,
						Flags = (uint) KeyboardFlag.Unicode,
						Time = 0,
						ExtraInfo = NativeInput.GetMessageExtraInfo()
					}
			}
		};

		// Handle extended keys:
		// If the scan code is preceded by a prefix byte that has the value 0xE0 (224),
		// we need to include the extended key flag in the Flags property. 
		if ((character & 0xFF00) == 0xE000)
		{
			input.Data.Keyboard.Flags |= (uint) KeyboardFlag.ExtendedKey;
		}

		// Add initial down, then modify and add up.
		_inputList.Add(input);
		input.Data.Keyboard.Flags |= (uint) KeyboardFlag.KeyUp;
		_inputList.Add(input);

		return this;
	}

	/// <summary>
	/// Adds a key down to the list of <see cref="InputTypeWithData" /> messages.
	/// </summary>
	/// <param name="keys"> The keys to press down. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public override InputBuilder AddKeyDown(params KeyboardKey[] keys)
	{
		keys.ForEach(key =>
		{
			var down =
				new InputTypeWithData
				{
					Type = (uint) InputType.Keyboard,
					Data =
					{
						Keyboard =
							new KeyboardInput
							{
								VirtualKeyCode = (ushort) key,
								ScanCode = (ushort) (NativeInput.MapVirtualKey((uint) key, 0) & 0xFFU),
								Flags = Keyboard.IsExtendedKey(key) ? (uint) KeyboardFlag.ExtendedKey : 0,
								Time = 0,
								ExtraInfo = IntPtr.Zero
							}
					}
				};

			_inputList.Add(down);
		});
		return this;
	}

	/// <summary>
	/// Adds a key up to the list of <see cref="InputTypeWithData" /> messages.
	/// </summary>
	/// <param name="keys"> The keys to release. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public override InputBuilder AddKeyUp(params KeyboardKey[] keys)
	{
		keys.ForEach(key =>
		{
			var up =
				new InputTypeWithData
				{
					Type = (uint) InputType.Keyboard,
					Data =
					{
						Keyboard =
							new KeyboardInput
							{
								VirtualKeyCode = (ushort) key,
								ScanCode = (ushort) (NativeInput.MapVirtualKey((uint) key, 0) & 0xFFU),
								Flags = (uint) (Keyboard.IsExtendedKey(key)
									? KeyboardFlag.KeyUp | KeyboardFlag.ExtendedKey
									: KeyboardFlag.KeyUp),
								Time = 0,
								ExtraInfo = IntPtr.Zero
							}
					}
				};

			_inputList.Add(up);
		});
		return this;
	}

	/// <inheritdoc />
	public override InputBuilder AddMouseButtonDown(params MouseButton[] buttons)
	{
		foreach (var button in buttons)
		{
			var buttonDown = new InputTypeWithData { Type = (uint) InputType.Mouse };
			buttonDown.Data.Mouse.Flags = (int) ToMouseButtonDownFlag(button);
			buttonDown.Data.Mouse.MouseData = button switch
			{
				MouseButton.XButton1 => 0x0001,
				MouseButton.XButton2 => 0x0002,
				_ => buttonDown.Data.Mouse.MouseData
			};
			_inputList.Add(buttonDown);
		}

		return this;
	}

	/// <summary>
	/// Adds a mouse button down for the specified button.
	/// </summary>
	/// <param name="button"> </param>
	/// <param name="x"> If relative is true then the relative amount of distance to move else it will be an absolute X position to move to. </param>
	/// <param name="y"> If relative is true then the relative amount of distance to move else it will be an absolute Y position to move to. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public override InputBuilder AddMouseButtonDrag(MouseButton button, int x, int y)
	{
		// Move relative movement of the mouse cursor
		var input = new InputTypeWithData { Type = (uint) InputType.Mouse };
		input.Data.Mouse.Flags = (int) ToMouseButtonDownFlag(button);

		_inputList.Add(input);

		// Move relative movement of the mouse cursor
		input = new InputTypeWithData { Type = (uint) InputType.Mouse };
		input.Data.Mouse.Flags = (int) (ToMouseButtonUpFlag(button) | MouseFlag.Move | MouseFlag.MoveNoCoalesce);
		input.Data.Mouse.X = x;
		input.Data.Mouse.Y = y;

		_inputList.Add(input);

		return this;
	}

	/// <inheritdoc />
	public override InputBuilder AddMouseButtonUp(params MouseButton[] buttons)
	{
		foreach (var button in buttons)
		{
			var buttonUp = new InputTypeWithData { Type = (uint) InputType.Mouse };
			buttonUp.Data.Mouse.Flags = (int) ToMouseButtonUpFlag(button);
			buttonUp.Data.Mouse.MouseData = button switch
			{
				MouseButton.XButton1 => 0x0001,
				MouseButton.XButton2 => 0x0002,
				_ => buttonUp.Data.Mouse.MouseData
			};

			_inputList.Add(buttonUp);
		}

		return this;
	}

	/// <summary>
	/// Scroll the horizontal mouse wheel by the specified amount.
	/// </summary>
	/// <param name="scrollAmount"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public override InputBuilder AddMouseHorizontalWheelScroll(int scrollAmount)
	{
		var scroll = new InputTypeWithData { Type = (uint) InputType.Mouse };
		scroll.Data.Mouse.Flags = (int) MouseFlag.HorizontalWheel;
		scroll.Data.Mouse.MouseData = scrollAmount;

		_inputList.Add(scroll);

		return this;
	}

	/// <summary>
	/// Moves the mouse relative to its current position or to an absolute position.
	/// </summary>
	/// <param name="x"> If relative is true then the relative amount of distance to move else it will be an absolute X position to move to. </param>
	/// <param name="y"> If relative is true then the relative amount of distance to move else it will be an absolute Y position to move to. </param>
	/// <param name="relative"> True to move a relative amount or false to move to an absolute position. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public override InputBuilder AddMouseMovement(int x, int y, bool relative)
	{
		if (relative)
		{
			// Move relative movement of the mouse cursor
			var input = new InputTypeWithData { Type = (uint) InputType.Mouse };
			input.Data.Mouse.Flags = (int) MouseFlag.Move;
			input.Data.Mouse.X = x;
			input.Data.Mouse.Y = y;

			_inputList.Add(input);
		}
		else
		{
			// Move the mouse to an absolute location (x,y)
			var screen = Screen.FromPoint(x, y);
			var relativeX = ((x * 65536) / screen.Size.Width) + 1;
			var relativeY = ((y * 65536) / screen.Size.Height) + 1;

			var movement = new InputTypeWithData { Type = (uint) InputType.Mouse };
			movement.Data.Mouse.Flags = (int) (MouseFlag.Move | MouseFlag.Absolute);
			movement.Data.Mouse.X = relativeX;
			movement.Data.Mouse.Y = relativeY;

			_inputList.Add(movement);
		}

		return this;
	}

	/// <summary>
	/// Move the mouse to the absolute position on the virtual desktop.
	/// </summary>
	/// <param name="absoluteX"> The absolute X position to move to. </param>
	/// <param name="absoluteY"> The absolute Y position to move to. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public override InputBuilder AddMouseMovementOnVirtualDesktop(int absoluteX, int absoluteY)
	{
		var screen = Screen.VirtualScreenSize;
		var relativeX = ((absoluteX * 65536) / screen.Size.Width) + 1;
		var relativeY = ((absoluteY * 65536) / screen.Size.Height) + 1;

		var movement = new InputTypeWithData { Type = (uint) InputType.Mouse };
		movement.Data.Mouse.Flags = (int) (MouseFlag.Move | MouseFlag.Absolute | MouseFlag.VirtualDesk);
		movement.Data.Mouse.X = relativeX;
		movement.Data.Mouse.Y = relativeY;

		_inputList.Add(movement);

		return this;
	}

	/// <summary>
	/// Scroll the vertical mouse wheel by the specified amount.
	/// </summary>
	/// <param name="scrollAmount"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public override InputBuilder AddMouseVerticalWheelScroll(int scrollAmount)
	{
		var scroll = new InputTypeWithData { Type = (uint) InputType.Mouse };
		scroll.Data.Mouse.Flags = (int) MouseFlag.VerticalWheel;
		scroll.Data.Mouse.MouseData = scrollAmount;

		_inputList.Add(scroll);

		return this;
	}

	/// <summary>
	/// Adds a mouse button down for the specified button.
	/// </summary>
	/// <param name="xButtonId"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public override InputBuilder AddMouseXButtonDown(int xButtonId)
	{
		var buttonDown = new InputTypeWithData { Type = (uint) InputType.Mouse };
		buttonDown.Data.Mouse.Flags = (int) MouseFlag.XDown;
		buttonDown.Data.Mouse.MouseData = xButtonId;

		_inputList.Add(buttonDown);

		return this;
	}

	/// <summary>
	/// Adds a mouse button up for the specified button.
	/// </summary>
	/// <param name="xButtonId"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public override InputBuilder AddMouseXButtonUp(int xButtonId)
	{
		var buttonUp = new InputTypeWithData { Type = (uint) InputType.Mouse };
		buttonUp.Data.Mouse.Flags = (int) MouseFlag.XUp;
		buttonUp.Data.Mouse.MouseData = xButtonId;

		_inputList.Add(buttonUp);

		return this;
	}

	/// <summary>
	/// Clear the input list.
	/// </summary>
	public override InputBuilder Clear()
	{
		_inputList.Clear();
		return this;
	}

	/// <summary>
	/// Initializes a keyboard state from a character value.
	/// </summary>
	public static KeyboardState FromCharacter(char? character)
	{
		var response = new KeyboardState();
		var vk = character != null ? NativeInput.VkKeyScan(character.Value) : 0;

		response.Character = character;
		response.IsLeftShiftPressed = (vk & 0x0100) == 0x0100;
		response.Key = (KeyboardKey) (vk & 0xFF);

		return response;
	}

	/// <summary>
	/// Returns the list of <see cref="InputTypeWithData" /> messages as a <see cref="System.Array" /> of <see cref="InputTypeWithData" /> messages.
	/// </summary>
	/// <returns> The <see cref="Array" /> of <see cref="InputTypeWithData" /> messages. </returns>
	internal InputTypeWithData[] ToArray()
	{
		return _inputList.ToArray();
	}

	private static MouseFlag ToMouseButtonDownFlag(MouseButton button)
	{
		return button switch
		{
			MouseButton.LeftButton => MouseFlag.LeftDown,
			MouseButton.MiddleButton => MouseFlag.MiddleDown,
			MouseButton.RightButton => MouseFlag.RightDown,
			MouseButton.XButton1 => MouseFlag.XDown,
			MouseButton.XButton2 => MouseFlag.XDown,
			_ => MouseFlag.None
		};
	}

	private static MouseFlag ToMouseButtonUpFlag(MouseButton button)
	{
		return button switch
		{
			MouseButton.LeftButton => MouseFlag.LeftUp,
			MouseButton.MiddleButton => MouseFlag.MiddleUp,
			MouseButton.RightButton => MouseFlag.RightUp,
			MouseButton.XButton1 => MouseFlag.XUp,
			MouseButton.XButton2 => MouseFlag.XUp,
			_ => MouseFlag.None
		};
	}

	#endregion
}