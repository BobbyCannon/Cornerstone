#region References

using System;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Input;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Foundation;
using GameController;

#endregion

namespace Cornerstone.Platforms.iOS;

/// <summary>
/// Reads iOS gamepads through GameController. Thumbstick Y is already positive up.
/// </summary>
[Preserve(AllMembers = true)]
[SourceReflection]
public class IOSGamepad : Gamepad
{
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private bool _hasReading;
	private StandardReading _lastReading;
	private bool _readFailed;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public IOSGamepad(IDateTimeProvider dateTimeProvider) : base(dateTimeProvider)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	#endregion

	#region Methods

	public override void Update()
	{
		try
		{
			var controller = FindController(State.Index);
			if (controller == null)
			{
				Disconnect();
				return;
			}

			var extended = controller.ExtendedGamepad;
			if (extended == null)
			{
				Disconnect();
				return;
			}

			var reading = ReadExtended(extended);
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

	private static GCController FindController(int index)
	{
		if (index < 0)
		{
			return null;
		}

		var controllers = GCController.Controllers;
		if (controllers == null)
		{
			return null;
		}

		var seen = 0;
		for (var i = 0; i < controllers.Length; i++)
		{
			var controller = controllers[i];
			if ((controller == null) || (controller.ExtendedGamepad == null))
			{
				continue;
			}

			if (seen == index)
			{
				return controller;
			}

			seen++;
		}

		return null;
	}

	private static StandardReading ReadExtended(GCExtendedGamepad gamepad)
	{
		var buttons = GamepadButton.None;
		SetButton(ref buttons, GamepadButton.A, gamepad.ButtonA);
		SetButton(ref buttons, GamepadButton.B, gamepad.ButtonB);
		SetButton(ref buttons, GamepadButton.X, gamepad.ButtonX);
		SetButton(ref buttons, GamepadButton.Y, gamepad.ButtonY);
		SetButton(ref buttons, GamepadButton.LeftBumper, gamepad.LeftShoulder);
		SetButton(ref buttons, GamepadButton.RightBumper, gamepad.RightShoulder);
		SetButton(ref buttons, GamepadButton.Back, gamepad.ButtonOptions);
		SetButton(ref buttons, GamepadButton.Start, gamepad.ButtonMenu);
		SetButton(ref buttons, GamepadButton.LeftStickThumb, gamepad.LeftThumbstickButton);
		SetButton(ref buttons, GamepadButton.RightStickThumb, gamepad.RightThumbstickButton);
		SetDpad(ref buttons, gamepad.DPad);

		return new StandardReading(
			buttons,
			Thumb(gamepad.LeftThumbstick, true),
			Thumb(gamepad.LeftThumbstick, false),
			Thumb(gamepad.RightThumbstick, true),
			Thumb(gamepad.RightThumbstick, false),
			Trigger(gamepad.LeftTrigger),
			Trigger(gamepad.RightTrigger));
	}

	private static void SetButton(ref GamepadButton buttons, GamepadButton flag, GCControllerButtonInput input)
	{
		if ((input != null) && input.IsPressed)
		{
			buttons |= flag;
		}
	}

	private static void SetDpad(ref GamepadButton buttons, GCControllerDirectionPad pad)
	{
		if (pad == null)
		{
			return;
		}

		SetButton(ref buttons, GamepadButton.DpadUp, pad.Up);
		SetButton(ref buttons, GamepadButton.DpadDown, pad.Down);
		SetButton(ref buttons, GamepadButton.DpadLeft, pad.Left);
		SetButton(ref buttons, GamepadButton.DpadRight, pad.Right);
	}

	private static short Thumb(GCControllerDirectionPad stick, bool horizontal)
	{
		if (stick == null)
		{
			return 0;
		}

		var axis = horizontal ? stick.XAxis : stick.YAxis;
		if (axis == null)
		{
			return 0;
		}

		return GamepadValues.ToThumb(axis.Value);
	}

	private static byte Trigger(GCControllerButtonInput input)
	{
		if (input == null)
		{
			return 0;
		}

		return GamepadValues.ToTrigger(input.Value);
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
