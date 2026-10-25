#region References

using System;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Input;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.MacOS;

/// <summary>
/// Reads macOS gamepads through GameController.framework. Thumbstick Y is already positive up.
/// </summary>
[SourceReflection]
public class MacOSGamepad : Gamepad
{
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private bool _hasReading;
	private StandardReading _lastReading;
	private bool _readFailed;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public MacOSGamepad(IDateTimeProvider dateTimeProvider) : base(dateTimeProvider)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	#endregion

	#region Methods

	public override void Update()
	{
		try
		{
			if (!NativeMacGameController.TryRead(State.Index, out var sample) || !sample.Found)
			{
				Disconnect();
				return;
			}

			var reading = ToReading(sample);
			if (_hasReading && _lastReading.Equals(reading))
			{
				return;
			}

			_hasReading = true;
			_lastReading = reading;
			Apply(reading);
		}
		catch (Exception ex)
		{
			if (!_readFailed)
			{
				_readFailed = true;
				Console.WriteLine(ex);
			}

			Disconnect();
		}
	}

	private void Apply(StandardReading reading)
	{
		var lastButtons = State.Buttons;
		State.IsConnected = true;
		State.Buttons = reading.Buttons;
		State.LeftTriggerValue = reading.LeftTrigger;
		State.RightTriggerValue = reading.RightTrigger;
		State.UpdateLeftThumb(reading.LeftX, reading.LeftY);
		State.UpdateRightThumb(reading.RightX, reading.RightY);
		State.UpdateTriggers();

		if (State.HasChanges())
		{
			State.DateTime = _dateTimeProvider.UtcNow;
			if (lastButtons != State.Buttons)
			{
				TriggerButtonEvents(lastButtons, State.Buttons);
			}

			OnChanged(State.ShallowClone());
			State.ResetHasChanges();
		}
	}

	private void Disconnect()
	{
		_hasReading = false;
		if (!State.IsConnected)
		{
			return;
		}

		State.Reset(_dateTimeProvider.UtcNow);
		OnChanged(State.ShallowClone());
	}

	private static void SetButton(ref GamepadButton buttons, GamepadButton flag, bool pressed)
	{
		if (pressed)
		{
			buttons |= flag;
		}
	}

	private static StandardReading ToReading(NativeMacGameController.ControllerSample sample)
	{
		var buttons = GamepadButton.None;
		SetButton(ref buttons, GamepadButton.A, sample.A);
		SetButton(ref buttons, GamepadButton.B, sample.B);
		SetButton(ref buttons, GamepadButton.X, sample.X);
		SetButton(ref buttons, GamepadButton.Y, sample.Y);
		SetButton(ref buttons, GamepadButton.LeftBumper, sample.LeftBumper);
		SetButton(ref buttons, GamepadButton.RightBumper, sample.RightBumper);
		SetButton(ref buttons, GamepadButton.Back, sample.Back);
		SetButton(ref buttons, GamepadButton.Start, sample.Start);
		SetButton(ref buttons, GamepadButton.LeftStickThumb, sample.LeftStick);
		SetButton(ref buttons, GamepadButton.RightStickThumb, sample.RightStick);
		SetButton(ref buttons, GamepadButton.DpadUp, sample.Up);
		SetButton(ref buttons, GamepadButton.DpadDown, sample.Down);
		SetButton(ref buttons, GamepadButton.DpadLeft, sample.Left);
		SetButton(ref buttons, GamepadButton.DpadRight, sample.Right);

		return new StandardReading(
			buttons,
			GamepadValues.ToThumb(sample.LeftX),
			GamepadValues.ToThumb(sample.LeftY),
			GamepadValues.ToThumb(sample.RightX),
			GamepadValues.ToThumb(sample.RightY),
			GamepadValues.ToTrigger(sample.LeftTrigger),
			GamepadValues.ToTrigger(sample.RightTrigger));
	}

	private void TriggerButtonEvents(GamepadButton lastButtons, GamepadButton stateButtons)
	{
		var oldButtons = lastButtons.GetFlagValues();
		var newButtons = stateButtons.GetFlagValues();

		foreach (var released in oldButtons.Except(newButtons))
		{
			OnButtonChanged(released, false);
		}

		foreach (var pressed in newButtons.Except(oldButtons))
		{
			OnButtonChanged(pressed, true);
		}
	}

	#endregion

	#region Structures

	private readonly struct StandardReading : IEquatable<StandardReading>
	{
		public StandardReading(
			GamepadButton buttons,
			short leftX,
			short leftY,
			short rightX,
			short rightY,
			byte leftTrigger,
			byte rightTrigger)
		{
			Buttons = buttons;
			LeftX = leftX;
			LeftY = leftY;
			RightX = rightX;
			RightY = rightY;
			LeftTrigger = leftTrigger;
			RightTrigger = rightTrigger;
		}

		public GamepadButton Buttons { get; }

		public short LeftX { get; }

		public short LeftY { get; }

		public short RightX { get; }

		public short RightY { get; }

		public byte LeftTrigger { get; }

		public byte RightTrigger { get; }

		public bool Equals(StandardReading other)
		{
			return (Buttons == other.Buttons)
				&& (LeftX == other.LeftX)
				&& (LeftY == other.LeftY)
				&& (RightX == other.RightX)
				&& (RightY == other.RightY)
				&& (LeftTrigger == other.LeftTrigger)
				&& (RightTrigger == other.RightTrigger);
		}
	}

	#endregion
}
