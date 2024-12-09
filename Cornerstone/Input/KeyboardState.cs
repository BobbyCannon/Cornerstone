#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Internal;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Input;

/// <summary>
/// Represents the state of the keyboard during Keyboard.StartMonitoring.
/// </summary>
public class KeyboardState
	: Bindable<KeyboardState>,
		IComparable<KeyboardState>,
		IComparable,
		IEquatable<KeyboardState>
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

	/// <inheritdoc />
	public int CompareTo(KeyboardState other)
	{
		return Equals(other) ? 0 : 1;
	}

	/// <inheritdoc />
	public int CompareTo(object obj)
	{
		return Equals(obj) ? 0 : 1;
	}

	/// <inheritdoc />
	public bool Equals(KeyboardState state)
	{
		if (state == null)
		{
			return false;
		}

		return
			(Character == state.Character) &&
			(DateTime == state.DateTime) &&
			(Event == state.Event) &&
			(IsAltPressed == state.IsAltPressed) &&
			(IsCapsLockOn == state.IsCapsLockOn) &&
			(IsControlPressed == state.IsControlPressed) &&
			(IsLeftAltPressed == state.IsLeftAltPressed) &&
			(IsLeftControlPressed == state.IsLeftControlPressed) &&
			(IsLeftShiftPressed == state.IsLeftShiftPressed) &&
			(IsPressed == state.IsPressed) &&
			(IsRightAltPressed == state.IsRightAltPressed) &&
			(IsRightControlPressed == state.IsRightControlPressed) &&
			(IsRightShiftPressed == state.IsRightShiftPressed) &&
			(IsShiftPressed == state.IsShiftPressed) &&
			(Key == state.Key);
	}

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		return Equals(obj as KeyboardState);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		var hashCode = HashCodeCalculator.Combine(
			Character, DateTime, Event, IsAltPressed, IsCapsLockOn,
			IsControlPressed, IsLeftAltPressed, IsLeftControlPressed, IsLeftShiftPressed,
			IsPressed, IsRightAltPressed, IsRightControlPressed, IsRightShiftPressed,
			IsShiftPressed, Key
		);
		return hashCode;
	}

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

	/// <inheritdoc />
	public override string ToString()
	{
		Character ??= Keyboard.ToCharacter(Key, this);
		return Character?.ToString();
	}

	public bool UpdateWith(ExpectedKeyState update, IncludeExcludeSettings settings)
	{
		IsAltPressed = update.IsAltRequired;
		IsControlPressed = update.IsControlRequired;
		IsLeftAltPressed = update.IsLeftAltRequired;
		IsLeftControlPressed = update.IsLeftControlRequired;
		IsLeftShiftPressed = update.IsLeftShiftRequired;
		IsRightAltPressed = update.IsRightAltRequired;
		IsRightControlPressed = update.IsRightControlRequired;
		IsRightShiftPressed = update.IsRightShiftRequired;
		IsShiftPressed = update.IsShiftRequired;
		Key = update.Key;
		return true;
	}

	/// <summary>
	/// Update the KeyboardState with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(KeyboardState update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			Character = update.Character;
			DateTime = update.DateTime;
			Event = update.Event;
			IsAltPressed = update.IsAltPressed;
			IsCapsLockOn = update.IsCapsLockOn;
			IsControlPressed = update.IsControlPressed;
			IsLeftAltPressed = update.IsLeftAltPressed;
			IsLeftControlPressed = update.IsLeftControlPressed;
			IsLeftShiftPressed = update.IsLeftShiftPressed;
			IsPressed = update.IsPressed;
			IsRightAltPressed = update.IsRightAltPressed;
			IsRightControlPressed = update.IsRightControlPressed;
			IsRightShiftPressed = update.IsRightShiftPressed;
			IsShiftPressed = update.IsShiftPressed;
			Key = update.Key;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Character)), x => x.Character = update.Character);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(DateTime)), x => x.DateTime = update.DateTime);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Event)), x => x.Event = update.Event);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsAltPressed)), x => x.IsAltPressed = update.IsAltPressed);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsCapsLockOn)), x => x.IsCapsLockOn = update.IsCapsLockOn);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsControlPressed)), x => x.IsControlPressed = update.IsControlPressed);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsLeftAltPressed)), x => x.IsLeftAltPressed = update.IsLeftAltPressed);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsLeftControlPressed)), x => x.IsLeftControlPressed = update.IsLeftControlPressed);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsLeftShiftPressed)), x => x.IsLeftShiftPressed = update.IsLeftShiftPressed);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsPressed)), x => x.IsPressed = update.IsPressed);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsRightAltPressed)), x => x.IsRightAltPressed = update.IsRightAltPressed);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsRightControlPressed)), x => x.IsRightControlPressed = update.IsRightControlPressed);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsRightShiftPressed)), x => x.IsRightShiftPressed = update.IsRightShiftPressed);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsShiftPressed)), x => x.IsShiftPressed = update.IsShiftPressed);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Key)), x => x.Key = update.Key);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			KeyboardState value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}