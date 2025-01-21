#region References

using System;
using System.Runtime.CompilerServices;
using Cornerstone.Data;
using Cornerstone.Internal;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Input;

/// <summary>
/// Represents the state of the gamepad during Gamepad.StartMonitoring.
/// </summary>
public class GamepadState
	: Bindable<GamepadState>,
		IComparable<GamepadState>,
		IComparable,
		IEquatable<GamepadState>
{
	#region Properties

	public bool ButtonA => Buttons.HasFlag(GamepadButton.A);

	public bool ButtonB => Buttons.HasFlag(GamepadButton.B);

	public bool ButtonBack => Buttons.HasFlag(GamepadButton.Back);

	public bool ButtonDpadDown => Buttons.HasFlag(GamepadButton.DpadDown);

	public bool ButtonDpadLeft => Buttons.HasFlag(GamepadButton.DpadLeft);

	public bool ButtonDpadRight => Buttons.HasFlag(GamepadButton.DpadRight);

	public bool ButtonDpadUp => Buttons.HasFlag(GamepadButton.DpadUp);

	public bool ButtonLeftBumper => Buttons.HasFlag(GamepadButton.LeftBumper);

	public bool ButtonLeftStick => Buttons.HasFlag(GamepadButton.LeftStick);

	public bool ButtonLeftTrigger => Buttons.HasFlag(GamepadButton.LeftTrigger);

	public bool ButtonRightBumper => Buttons.HasFlag(GamepadButton.RightBumper);

	public bool ButtonRightStick => Buttons.HasFlag(GamepadButton.RightStick);

	public bool ButtonRightTrigger => Buttons.HasFlag(GamepadButton.RightTrigger);

	/// <summary>
	/// All button states
	/// </summary>
	public GamepadButton Buttons { get; set; }

	public bool ButtonStart => Buttons.HasFlag(GamepadButton.Start);

	public bool ButtonX => Buttons.HasFlag(GamepadButton.X);

	public bool ButtonY => Buttons.HasFlag(GamepadButton.Y);

	/// <summary>
	/// The date and time the change occured.
	/// </summary>
	public DateTime DateTime { get; set; }

	/// <summary>
	/// True if the gamepad is connected otherwise false.
	/// </summary>
	public bool IsConnected { get; set; }

	public MovementDelta LeftMovementDelta { get; set; }

	/// <summary>
	/// Left thumbstick x-axis value.
	/// The value is between -32768 and 32767.
	/// </summary>
	public short LeftThumbX { get; set; }

	/// <summary>
	/// Left thumbstick y-axis value.
	/// The value is between -32768 and 32767.
	/// </summary>
	public short LeftThumbY { get; set; }

	/// <summary>
	/// The current value of the left trigger analog control.
	/// The value is between 0 and 100.
	/// </summary>
	public float LeftTriggerPercent => ConvertTriggerToPercent(LeftTriggerValue);

	/// <summary>
	/// The current value of the left trigger analog control.
	/// The value is between 0 and 255.
	/// </summary>
	public byte LeftTriggerValue { get; set; }

	/// <summary>
	/// Left thumbstick x-axis percent value.
	/// The value is between -1 and 1.
	/// </summary>
	public float PercentOfLeftThumbX => ConvertThumbToPercent(LeftThumbX);

	/// <summary>
	/// Left thumbstick y-axis percent value.
	/// The value is between -1 and 1.
	/// </summary>
	public float PercentOfLeftThumbY => ConvertThumbToPercent(LeftThumbY);

	/// <summary>
	/// Right thumbstick x-axis percent value.
	/// The value is between -1 and 1.
	/// </summary>
	public float PercentOfRightThumbX => ConvertThumbToPercent(RightThumbX);

	/// <summary>
	/// Right thumbstick y-axis percent value.
	/// The value is between -1 and 1.
	/// </summary>
	public float PercentOfRightThumbY => ConvertThumbToPercent(RightThumbY);

	public MovementDelta RightMovementDelta { get; set; }

	/// <summary>
	/// Is the right thumb is pushed passed the percent.
	/// </summary>
	public bool RightThumbPushed =>
		(Math.Abs(PercentOfRightThumbX) > 0.1f)
		|| (Math.Abs(PercentOfRightThumbY) > 0.1f);

	/// <summary>
	/// Right thumbstick x-axis value.
	/// The value is between -32768 and 32767.
	/// </summary>
	public short RightThumbX { get; set; }

	/// <summary>
	/// Right thumbstick y-axis value.
	/// The value is between -32768 and 32767.
	/// </summary>
	public short RightThumbY { get; set; }

	/// <summary>
	/// The current value of the right trigger analog control.
	/// The value is between 0 and 100.
	/// </summary>
	public float RightTriggerPercent => ConvertTriggerToPercent(RightTriggerValue);

	/// <summary>
	/// The current value of the right trigger analog control.
	/// The value is between 0 and 255.
	/// </summary>
	public byte RightTriggerValue { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public int CompareTo(GamepadState other)
	{
		return Equals(other) ? 0 : 1;
	}

	/// <inheritdoc />
	public int CompareTo(object obj)
	{
		return Equals(obj as GamepadState) ? 0 : 1;
	}

	/// <inheritdoc />
	public bool Equals(GamepadState state)
	{
		if (state == null)
		{
			return false;
		}

		return (Buttons == state.Buttons)
			&& (DateTime == state.DateTime)
			&& (IsConnected == state.IsConnected)
			&& (LeftThumbX == state.LeftThumbX)
			&& (LeftThumbY == state.LeftThumbY)
			&& (LeftTriggerValue == state.LeftTriggerValue)
			&& (RightThumbX == state.RightThumbX)
			&& (RightThumbY == state.RightThumbY)
			&& (RightTriggerValue == state.RightTriggerValue);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		var hashCode = HashCodeCalculator.Combine(
			Buttons, DateTime, IsConnected,
			LeftThumbX, LeftThumbY, LeftTriggerValue,
			RightThumbX, RightThumbY, RightTriggerValue
		);
		return hashCode;
	}

	public void Reset(DateTime dateTime)
	{
		Buttons = GamepadButton.None;
		DateTime = dateTime;
		IsConnected = false;
		LeftThumbX = 0;
		LeftThumbY = 0;
		LeftTriggerValue = 0;
		RightThumbX = 0;
		RightThumbY = 0;
		RightTriggerValue = 0;
	}

	public void UpdateLeftThumb(short x, short y)
	{
		LeftMovementDelta = MovementDelta.FromPosition(LeftThumbX, LeftThumbY, x, y);
		LeftThumbX = x;
		LeftThumbY = y;
	}

	public void UpdateRightThumb(short x, short y)
	{
		RightMovementDelta = MovementDelta.FromPosition(RightThumbX, RightThumbY, x, y);
		RightThumbX = x;
		RightThumbY = y;
	}

	/// <inheritdoc />
	public override bool UpdateWith(GamepadState update, IncludeExcludeSettings settings)
	{
		// Code Generated - UpdateWith

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(Buttons, update.Buttons, settings.ShouldProcessProperty(nameof(Buttons)), x => Buttons = x);
		UpdateProperty(DateTime, update.DateTime, settings.ShouldProcessProperty(nameof(DateTime)), x => DateTime = x);
		UpdateProperty(IsConnected, update.IsConnected, settings.ShouldProcessProperty(nameof(IsConnected)), x => IsConnected = x);
		UpdateProperty(LeftThumbX, update.LeftThumbX, settings.ShouldProcessProperty(nameof(LeftThumbX)), x => LeftThumbX = x);
		UpdateProperty(LeftThumbY, update.LeftThumbY, settings.ShouldProcessProperty(nameof(LeftThumbY)), x => LeftThumbY = x);
		UpdateProperty(LeftTriggerValue, update.LeftTriggerValue, settings.ShouldProcessProperty(nameof(LeftTriggerValue)), x => LeftTriggerValue = x);
		UpdateProperty(RightThumbX, update.RightThumbX, settings.ShouldProcessProperty(nameof(RightThumbX)), x => RightThumbX = x);
		UpdateProperty(RightThumbY, update.RightThumbY, settings.ShouldProcessProperty(nameof(RightThumbY)), x => RightThumbY = x);
		UpdateProperty(RightTriggerValue, update.RightTriggerValue, settings.ShouldProcessProperty(nameof(RightTriggerValue)), x => RightTriggerValue = x);

		// Code Generated - /UpdateWith

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			GamepadState value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	/// <summary>
	/// Truncates the specified <see cref="float" /> value to the -1 to 1
	/// inclusive range.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float ClampPercent(float value)
	{
		return float.IsNaN(value) ? 0 : value < -1f ? -1f : value > 1f ? 1f : value;
	}

	private float ConvertThumbToPercent(short value)
	{
		return ClampPercent((float) value / (value >= 0 ? 32767 : 32768));
	}

	private float ConvertTriggerToPercent(short value)
	{
		return (float) value / (value >= 0 ? 254 : 255);
	}

	#endregion
}