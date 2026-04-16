#region References

using System;
using System.Runtime.CompilerServices;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Input;

/// <summary>
/// Represents the state of the gamepad during Gamepad.StartMonitoring.
/// </summary>
public partial class GamepadState
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

	public bool ButtonLeftStick => Buttons.HasFlag(GamepadButton.LeftStickThumb);

	public bool ButtonLeftStickDown => Buttons.HasFlag(GamepadButton.LeftStickDown);

	public bool ButtonLeftStickLeft => Buttons.HasFlag(GamepadButton.LeftStickLeft);

	public bool ButtonLeftStickRight => Buttons.HasFlag(GamepadButton.LeftStickRight);

	public bool ButtonLeftStickUp => Buttons.HasFlag(GamepadButton.LeftStickUp);

	public bool ButtonLeftTrigger => Buttons.HasFlag(GamepadButton.LeftTrigger);

	public bool ButtonRightBumper => Buttons.HasFlag(GamepadButton.RightBumper);

	public bool ButtonRightStick => Buttons.HasFlag(GamepadButton.RightStickThumb);

	public bool ButtonRightStickDown => Buttons.HasFlag(GamepadButton.RightStickDown);

	public bool ButtonRightStickLeft => Buttons.HasFlag(GamepadButton.RightStickLeft);

	public bool ButtonRightStickRight => Buttons.HasFlag(GamepadButton.RightStickRight);

	public bool ButtonRightStickUp => Buttons.HasFlag(GamepadButton.RightStickUp);

	public bool ButtonRightTrigger => Buttons.HasFlag(GamepadButton.RightTrigger);

	/// <summary>
	/// All button states
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(ButtonA), nameof(ButtonB), nameof(ButtonBack), nameof(ButtonDpadDown), nameof(ButtonDpadLeft),
		nameof(ButtonDpadRight), nameof(ButtonDpadUp), nameof(ButtonLeftBumper), nameof(ButtonLeftStick),
		nameof(ButtonLeftStickLeft), nameof(ButtonLeftStickRight), nameof(ButtonLeftStickUp), nameof(ButtonLeftStickDown),
		nameof(ButtonLeftTrigger), nameof(ButtonRightBumper), nameof(ButtonRightStick),
		nameof(ButtonRightStickLeft), nameof(ButtonRightStickRight), nameof(ButtonRightStickUp), nameof(ButtonRightStickDown),
		nameof(ButtonRightTrigger), nameof(ButtonStart), nameof(ButtonX), nameof(ButtonY))]
	[UpdateableAction(UpdateableAction.All)]
	public partial GamepadButton Buttons { get; set; }

	public bool ButtonStart => Buttons.HasFlag(GamepadButton.Start);

	public bool ButtonX => Buttons.HasFlag(GamepadButton.X);

	public bool ButtonY => Buttons.HasFlag(GamepadButton.Y);

	/// <summary>
	/// The date and time the change occured.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial DateTime DateTime { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial double DeadZoneForThumb { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial double DeadZoneForTrigger { get; set; }

	/// <summary>
	/// The index of the controller.
	/// </summary>
	public int Index { get; set; }

	/// <summary>
	/// True if the gamepad is connected otherwise false.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsConnected { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial MovementDelta LeftMovementDelta { get; set; }

	/// <summary>
	/// Is the right thumb is pushed passed the percent.
	/// </summary>
	public bool LeftThumbPushed =>
		(Math.Abs(PercentOfLeftThumbX) > 0.1f)
		|| (Math.Abs(PercentOfLeftThumbY) > 0.1f);

	/// <summary>
	/// Left thumbstick x-axis value.
	/// The value is between -32768 and 32767.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(PercentOfLeftThumbX), nameof(LeftThumbPushed))]
	[UpdateableAction(UpdateableAction.All)]
	public partial short LeftThumbX { get; set; }

	/// <summary>
	/// Left thumbstick y-axis value.
	/// The value is between -32768 and 32767.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(PercentOfLeftThumbY))]
	[UpdateableAction(UpdateableAction.All)]
	public partial short LeftThumbY { get; set; }

	/// <summary>
	/// The current value of the left trigger analog control.
	/// The value is between 0.0 and 1.0.
	/// </summary>
	public float LeftTriggerPercent => ConvertTriggerToPercent(LeftTriggerValue);

	/// <summary>
	/// The current value of the left trigger analog control.
	/// The value is between 0 and 255.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(LeftTriggerPercent))]
	[UpdateableAction(UpdateableAction.All)]
	public partial byte LeftTriggerValue { get; set; }

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

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial MovementDelta RightMovementDelta { get; set; }

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
	[Notify]
	[AlsoNotify(nameof(PercentOfRightThumbX), nameof(RightThumbPushed))]
	[UpdateableAction(UpdateableAction.All)]
	public partial short RightThumbX { get; set; }

	/// <summary>
	/// Right thumbstick y-axis value.
	/// The value is between -32768 and 32767.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(PercentOfRightThumbY))]
	[UpdateableAction(UpdateableAction.All)]
	public partial short RightThumbY { get; set; }

	/// <summary>
	/// The current value of the right trigger analog control.
	/// The value is between 0.0 and 1.0.
	/// </summary>
	public float RightTriggerPercent => ConvertTriggerToPercent(RightTriggerValue);

	/// <summary>
	/// The current value of the right trigger analog control.
	/// The value is between 0 and 255.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(RightTriggerPercent))]
	[UpdateableAction(UpdateableAction.All)]
	public partial byte RightTriggerValue { get; set; }

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
	public bool Equals(GamepadState other)
	{
		if (other is null)
		{
			return false;
		}
		if (ReferenceEquals(this, other))
		{
			return true;
		}
		return (Buttons == other.Buttons)
			&& DateTime.Equals(other.DateTime)
			&& (IsConnected == other.IsConnected)
			&& LeftMovementDelta.Equals(other.LeftMovementDelta)
			&& (LeftThumbX == other.LeftThumbX)
			&& (LeftThumbY == other.LeftThumbY)
			&& (LeftTriggerValue == other.LeftTriggerValue)
			&& RightMovementDelta.Equals(other.RightMovementDelta)
			&& (RightThumbX == other.RightThumbX)
			&& (RightThumbY == other.RightThumbY)
			&& (RightTriggerValue == other.RightTriggerValue)
			&& DoubleExtensions.AreEqual(DeadZoneForThumb, other.DeadZoneForThumb)
			&& DoubleExtensions.AreEqual(DeadZoneForTrigger, other.DeadZoneForTrigger);
	}

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		if (obj is null)
		{
			return false;
		}
		if (ReferenceEquals(this, obj))
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((GamepadState) obj);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		var hashCode = new HashCode();
		hashCode.Add((int) Buttons);
		hashCode.Add(DateTime);
		hashCode.Add(IsConnected);
		hashCode.Add(LeftMovementDelta);
		hashCode.Add(LeftThumbX);
		hashCode.Add(LeftThumbY);
		hashCode.Add(LeftTriggerValue);
		hashCode.Add(RightMovementDelta);
		hashCode.Add(RightThumbX);
		hashCode.Add(RightThumbY);
		hashCode.Add(RightTriggerValue);
		hashCode.Add(DeadZoneForThumb);
		hashCode.Add(DeadZoneForTrigger);
		return hashCode.ToHashCode();
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
		DeadZoneForThumb = 0.25;
		DeadZoneForTrigger = 0.1;
	}

	public void UpdateLeftThumb(short x, short y)
	{
		LeftMovementDelta = MovementDelta.FromPosition(LeftThumbX, LeftThumbY, x, y);
		LeftThumbX = x;
		LeftThumbY = y;

		if (PercentOfLeftThumbX < -DeadZoneForThumb)
		{
			Buttons = Buttons.SetFlag(GamepadButton.LeftStickLeft);
		}
		else if (PercentOfLeftThumbX > DeadZoneForThumb)
		{
			Buttons = Buttons.SetFlag(GamepadButton.LeftStickRight);
		}

		if (PercentOfLeftThumbY > DeadZoneForThumb)
		{
			Buttons = Buttons.SetFlag(GamepadButton.LeftStickUp);
		}
		else if (PercentOfLeftThumbY < -DeadZoneForThumb)
		{
			Buttons = Buttons.SetFlag(GamepadButton.LeftStickDown);
		}
	}

	public void UpdateRightThumb(short x, short y)
	{
		RightMovementDelta = MovementDelta.FromPosition(RightThumbX, RightThumbY, x, y);
		RightThumbX = x;
		RightThumbY = y;

		if (PercentOfRightThumbX < -DeadZoneForThumb)
		{
			Buttons = Buttons.SetFlag(GamepadButton.RightStickLeft);
		}
		else if (PercentOfRightThumbX > DeadZoneForThumb)
		{
			Buttons = Buttons.SetFlag(GamepadButton.RightStickRight);
		}

		if (PercentOfRightThumbY > DeadZoneForThumb)
		{
			Buttons = Buttons.SetFlag(GamepadButton.RightStickUp);
		}
		else if (PercentOfRightThumbY < -DeadZoneForThumb)
		{
			Buttons = Buttons.SetFlag(GamepadButton.RightStickDown);
		}
	}

	public void UpdateTriggers()
	{
		if (LeftTriggerPercent >= DeadZoneForTrigger)
		{
			Buttons = Buttons.SetFlag(GamepadButton.LeftTrigger);
		}

		if (RightTriggerPercent >= DeadZoneForTrigger)
		{
			Buttons = Buttons.SetFlag(GamepadButton.RightTrigger);
		}
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