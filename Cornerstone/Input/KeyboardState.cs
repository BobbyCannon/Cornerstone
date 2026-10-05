#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Input;

/// <summary>
/// Represents the state of the keyboard during Keyboard.StartMonitoring.
/// </summary>
[SourceReflection]
public partial class KeyboardState
	: CornerstoneObject,
		IComparable<KeyboardState>,
		IComparable
{
	#region Properties

	/// <summary>
	/// The string interpretation of the key.
	/// </summary>
	public char? Character { get; set; }

	/// <summary>
	/// The date and time the change occured.
	/// </summary>
	public DateTime DateTime { get; set; }

	/// <summary>
	/// The keyboard event.
	/// </summary>
	public KeyboardEvent Event { get; set; }

	/// <summary>
	/// Gets a value indicating if either the left or right alt key is pressed.
	/// </summary>
	public bool IsAltPressed { get; set; }

	/// <summary>
	/// Determines if the caps lock in on at the time of the key event.
	/// </summary>
	public bool IsCapsLockOn { get; set; }

	/// <summary>
	/// Gets a value indicating if either the left or right control key is pressed.
	/// </summary>
	public bool IsControlPressed { get; set; }

	/// <summary>
	/// Gets a value indicating if the left alt key is pressed.
	/// </summary>
	public bool IsLeftAltPressed { get; set; }

	/// <summary>
	/// Gets a value indicating if the left control key is pressed.
	/// </summary>
	public bool IsLeftControlPressed { get; set; }

	/// <summary>
	/// Gets a value indicating if the left shift key is pressed.
	/// </summary>
	public bool IsLeftShiftPressed { get; set; }

	/// <summary>
	/// Gets a value indicating the key is being pressed (down). If false the key is being released (up).
	/// </summary>
	public bool IsPressed { get; set; }

	/// <summary>
	/// Gets a value indicating if the right alt key is pressed.
	/// </summary>
	public bool IsRightAltPressed { get; set; }

	/// <summary>
	/// Gets a value indicating if the right control key is pressed.
	/// </summary>
	public bool IsRightControlPressed { get; set; }

	/// <summary>
	/// Gets a value indicating if the right shift key is pressed.
	/// </summary>
	public bool IsRightShiftPressed { get; set; }

	/// <summary>
	/// Gets a value indicating if either the left or right shift key is pressed.
	/// </summary>
	public bool IsShiftPressed { get; set; }

	/// <summary>
	/// Gets a value of the key being changed (up or down).
	/// </summary>
	public KeyboardKey Key { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets the keyboard modifier for this state.
	/// </summary>
	/// <returns> The keyboard modifier. </returns>
	public KeyboardModifier GetKeyboardModifier()
	{
		var response = KeyboardModifier.None;

		if (IsLeftShiftPressed)
		{
			response |= KeyboardModifier.LeftShift;
		}

		if (IsRightShiftPressed)
		{
			response |= KeyboardModifier.RightShift;
		}

		if (IsLeftControlPressed)
		{
			response |= KeyboardModifier.LeftControl;
		}

		if (IsRightControlPressed)
		{
			response |= KeyboardModifier.RightControl;
		}

		if (IsLeftAltPressed)
		{
			response |= KeyboardModifier.LeftAlt;
		}

		if (IsRightAltPressed)
		{
			response |= KeyboardModifier.RightAlt;
		}

		return response;
	}

	/// <summary>
	/// To a details string for this keyboard state.
	/// </summary>
	/// <returns> </returns>
	public string ToDetailedString()
	{
		return $"Key: {Key}, Character: {Character}";
	}

	public override string ToString()
	{
		Character ??= Keyboard.ToCharacter(Key, this);
		return Character?.ToString();
	}

	#endregion
}