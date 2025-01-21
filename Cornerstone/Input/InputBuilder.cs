#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Input;

/// <summary>
/// A helper class for building a list of inputs to process.
/// </summary>
public abstract class InputBuilder
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="InputBuilder" /> class.
	/// </summary>
	/// <param name="textInputAsKeyPresses"> Defaults the keyboard SendInput to send text as key presses if true. </param>
	protected InputBuilder(bool textInputAsKeyPresses = true)
	{
		TextInputAsKeyPresses = textInputAsKeyPresses;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Defaults the keyboard SendInput to send text as key presses if true.
	/// Otherwise, the send input will send the text as a text input not as key presses.
	/// </summary>
	public bool TextInputAsKeyPresses { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Adds the character to the builder.
	/// </summary>
	/// <param name="character"> The character to be added an input. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder Add(char character);

	/// <summary>
	/// Adds the characters in the specified <see cref="IEnumerable{T}" /> of <see cref="char" />.
	/// </summary>
	/// <param name="characters"> The characters to add. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public InputBuilder Add(IEnumerable<char> characters)
	{
		characters.ForEach(x => Add(x));
		return this;
	}

	/// <summary>
	/// Adds the characters in the provided <see cref="string" />.
	/// </summary>
	/// <param name="text"> The text to add. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public InputBuilder Add(string text)
	{
		return Add(text.ToCharArray());
	}

	/// <summary>
	/// Add keystroke to the input builder.
	/// </summary>
	/// <param name="strokes"> The stroke(s) to add. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public InputBuilder Add(params KeyStroke[] strokes)
	{
		foreach (var stroke in strokes)
		{
			AddKeyStroke(stroke);
		}

		return this;
	}

	/// <summary>
	/// Adds a key down to the input builder.
	/// </summary>
	/// <param name="keys"> The keys to press down. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder AddKeyDown(params KeyboardKey[] keys);

	/// <summary>
	/// Adds a key press to the InputBuilder which is equivalent to a key down followed by a key up.
	/// </summary>
	/// <param name="states"> The keyboard states to convert to key press down. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public InputBuilder AddKeyPress(params KeyboardState[] states)
	{
		foreach (var state in states)
		{
			var modifier = state.GetKeyboardModifier();
			AddKeyPress(modifier, state.Key);
		}

		return this;
	}

	/// <summary>
	/// Adds a key press to the InputBuilder which is equivalent to a key down followed by a key up.
	/// </summary>
	/// <param name="keys"> The keys to press down. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public InputBuilder AddKeyPress(params KeyboardKey[] keys)
	{
		keys.ForEach(x =>
		{
			AddKeyDown(x);
			AddKeyUp(x);
		});
		return this;
	}

	/// <summary>
	/// Adds modified keystroke(s) where there are multiple modifiers and multiple keys like CTRL-ALT-K-C where CTRL
	/// and ALT are the modifierKeys and K and C are the keys.
	/// The flow is Modifiers KeyDown in order, Keys Press in order, Modifiers KeyUp in reverse order.
	/// </summary>
	/// <param name="modifier"> The modifier key. </param>
	/// <param name="keys"> The list of keys to press. </param>
	public InputBuilder AddKeyPress(KeyboardModifier modifier, params KeyboardKey[] keys)
	{
		var modifierKeys = ConvertModifierToKey(modifier).ToArray();
		AddKeyDown(modifierKeys);
		AddKeyPress(keys.ToArray());
		AddKeyUp(modifierKeys);
		return this;
	}

	/// <summary>
	/// Adds modified keystroke(s) where there are multiple modifiers and multiple keys like CTRL-ALT-K-C where CTRL
	/// and ALT are the modifierKeys and K and C are the keys.
	/// The flow is Modifiers KeyDown in order, Keys Press in order, Modifiers KeyUp in reverse order.
	/// </summary>
	/// <param name="modifiers"> The list of modifier keys </param>
	/// <param name="keys"> The list of keys to simulate </param>
	public InputBuilder AddKeyPress(IEnumerable<KeyboardModifier> modifiers, params KeyboardKey[] keys)
	{
		var modifierKeys = ConvertModifierToKey(modifiers).ToArray();
		AddKeyDown(modifierKeys);
		AddKeyPress(keys.ToArray());
		AddKeyUp(modifierKeys);
		return this;
	}

	/// <summary>
	/// Adds a key up to the InputBuilder.
	/// </summary>
	/// <param name="keys"> The keys to release. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder AddKeyUp(params KeyboardKey[] keys);

	/// <summary>
	/// Adds a single click of the specified button.
	/// </summary>
	/// <param name="button"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public InputBuilder AddMouseButtonClick(MouseButton button)
	{
		return AddMouseButtonDown(button).AddMouseButtonUp(button);
	}

	/// <summary>
	/// Adds a double click of the specified button.
	/// </summary>
	/// <param name="button"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public InputBuilder AddMouseButtonDoubleClick(MouseButton button)
	{
		return AddMouseButtonClick(button).AddMouseButtonClick(button);
	}

	/// <summary>
	/// Adds a mouse button down for the specified button.
	/// </summary>
	/// <param name="buttons"> The buttons to press down. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder AddMouseButtonDown(params MouseButton[] buttons);

	/// <summary>
	/// Adds a mouse button down for the specified button.
	/// </summary>
	/// <param name="button"> </param>
	/// <param name="x"> If relative is true then the relative amount of distance to move else it will be an absolute X position to move to. </param>
	/// <param name="y"> If relative is true then the relative amount of distance to move else it will be an absolute Y position to move to. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder AddMouseButtonDrag(MouseButton button, int x, int y);

	/// <summary>
	/// Adds a mouse button up for the specified button.
	/// </summary>
	/// <param name="buttons"> The buttons to press down. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder AddMouseButtonUp(params MouseButton[] buttons);

	/// <summary>
	/// Scroll the horizontal mouse wheel by the specified amount.
	/// </summary>
	/// <param name="scrollAmount"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder AddMouseHorizontalWheelScroll(int scrollAmount);

	/// <summary>
	/// Moves the mouse relative to its current position or to an absolute position.
	/// </summary>
	/// <param name="x"> If relative is true then the relative amount of distance to move else it will be an absolute X position to move to. </param>
	/// <param name="y"> If relative is true then the relative amount of distance to move else it will be an absolute Y position to move to. </param>
	/// <param name="relative"> True to move a relative amount or false to move to an absolute position. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder AddMouseMovement(int x, int y, bool relative);

	/// <summary>
	/// Move the mouse to the absolute position on the virtual desktop.
	/// </summary>
	/// <param name="absoluteX"> The absolute X position to move to. </param>
	/// <param name="absoluteY"> The absolute Y position to move to. </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder AddMouseMovementOnVirtualDesktop(int absoluteX, int absoluteY);

	/// <summary>
	/// Scroll the vertical mouse wheel by the specified amount.
	/// </summary>
	/// <param name="scrollAmount"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder AddMouseVerticalWheelScroll(int scrollAmount);

	/// <summary>
	/// Adds a single click of the specified button.
	/// </summary>
	/// <param name="xButtonId"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public InputBuilder AddMouseXButtonClick(int xButtonId)
	{
		return AddMouseXButtonDown(xButtonId).AddMouseXButtonUp(xButtonId);
	}

	/// <summary>
	/// Adds a double click of the specified button.
	/// </summary>
	/// <param name="xButtonId"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public InputBuilder AddMouseXButtonDoubleClick(int xButtonId)
	{
		return AddMouseXButtonClick(xButtonId).AddMouseXButtonClick(xButtonId);
	}

	/// <summary>
	/// Adds a mouse button down for the specified button.
	/// </summary>
	/// <param name="xButtonId"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder AddMouseXButtonDown(int xButtonId);

	/// <summary>
	/// Adds a mouse button up for the specified button.
	/// </summary>
	/// <param name="xButtonId"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	public abstract InputBuilder AddMouseXButtonUp(int xButtonId);

	/// <summary>
	/// Clear the input list.
	/// </summary>
	public abstract InputBuilder Clear();

	/// <summary>
	/// Add keystrokes to input builder.
	/// </summary>
	/// <param name="stroke"> </param>
	/// <returns> This <see cref="InputBuilder" /> instance. </returns>
	private InputBuilder AddKeyStroke(KeyStroke stroke)
	{
		var modifierKeys = ConvertModifierToKey(stroke.Modifier).ToList();

		foreach (var key in modifierKeys)
		{
			AddKeyDown(key);
		}

		switch (stroke.Action)
		{
			case KeyboardAction.KeyDown:
			{
				AddKeyDown(stroke.Key);
				break;
			}
			case KeyboardAction.KeyUp:
			{
				AddKeyUp(stroke.Key);
				break;
			}
			case KeyboardAction.KeyPress:
			default:
			{
				AddKeyPress(stroke.Key);
				break;
			}
		}

		foreach (var key in modifierKeys)
		{
			AddKeyUp(key);
		}

		return this;
	}

	/// <summary>
	/// Converts the modifier to the key.
	/// </summary>
	/// <param name="modifiers"> The modifiers to convert. </param>
	/// <returns> The key representation of the modifier. </returns>
	private static IEnumerable<KeyboardKey> ConvertModifierToKey(IEnumerable<KeyboardModifier> modifiers)
	{
		return ConvertModifierToKey(modifiers.ToArray());
	}

	/// <summary>
	/// Converts the modifier to the key.
	/// </summary>
	/// <param name="modifiers"> The modifiers to convert. </param>
	/// <returns> The key representation of the modifier. </returns>
	private static IEnumerable<KeyboardKey> ConvertModifierToKey(params KeyboardModifier[] modifiers)
	{
		if (modifiers.Any(x => x.HasFlag(KeyboardModifier.Alt)))
		{
			yield return KeyboardKey.Alt;
		}

		if (modifiers.Any(x => x.HasFlag(KeyboardModifier.LeftAlt)))
		{
			yield return KeyboardKey.LeftAlt;
		}

		if (modifiers.Any(x => x.HasFlag(KeyboardModifier.RightAlt)))
		{
			yield return KeyboardKey.RightAlt;
		}

		if (modifiers.Any(x => x.HasFlag(KeyboardModifier.Control)))
		{
			yield return KeyboardKey.Control;
		}

		if (modifiers.Any(x => x.HasFlag(KeyboardModifier.LeftControl)))
		{
			yield return KeyboardKey.LeftControl;
		}

		if (modifiers.Any(x => x.HasFlag(KeyboardModifier.RightControl)))
		{
			yield return KeyboardKey.RightControl;
		}

		if (modifiers.Any(x => x.HasFlag(KeyboardModifier.Shift)))
		{
			yield return KeyboardKey.Shift;
		}

		if (modifiers.Any(x => x.HasFlag(KeyboardModifier.LeftShift)))
		{
			yield return KeyboardKey.LeftShift;
		}

		if (modifiers.Any(x => x.HasFlag(KeyboardModifier.RightShift)))
		{
			yield return KeyboardKey.RightShift;
		}
	}

	#endregion
}