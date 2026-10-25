#region References

using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Cornerstone.Extensions;
using Cornerstone.Input;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Browser;

[SourceReflection]
public class BrowserGamepad : Gamepad
{
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private string _lastPayload;
	private bool _readFailed;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public BrowserGamepad(IDateTimeProvider dateTimeProvider) : base(dateTimeProvider)
	{
		_dateTimeProvider = dateTimeProvider;
		_lastPayload = string.Empty;
	}

	#endregion

	#region Methods

	public override void Update()
	{
		var payload = ReadPayload();
		if (payload.Length == 0)
		{
			if (State.IsConnected)
			{
				_lastPayload = string.Empty;
				State.Reset(_dateTimeProvider.UtcNow);
				OnChanged(State.ShallowClone());
			}

			return;
		}

		if (payload == _lastPayload)
		{
			return;
		}

		if (!TryRead(payload, out var reading))
		{
			return;
		}

		_lastPayload = payload;
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

	private string ReadPayload()
	{
		try
		{
			return BrowserGamepadInterop.Read(State.Index) ?? string.Empty;
		}
		catch (Exception ex)
		{
			if (!_readFailed)
			{
				_readFailed = true;
				Console.WriteLine(ex);
			}

			return string.Empty;
		}
	}

	private static bool TryRead(string payload, out StandardReading reading)
	{
		reading = default;
		var parts = payload.Split(',');
		if (parts.Length < 20)
		{
			return false;
		}

		var values = new double[20];
		for (var i = 0; i < values.Length; i++)
		{
			if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
			{
				return false;
			}
		}

		var buttons = GamepadButton.None;
		SetButton(ref buttons, GamepadButton.A, values[0]);
		SetButton(ref buttons, GamepadButton.B, values[1]);
		SetButton(ref buttons, GamepadButton.X, values[2]);
		SetButton(ref buttons, GamepadButton.Y, values[3]);
		SetButton(ref buttons, GamepadButton.LeftBumper, values[4]);
		SetButton(ref buttons, GamepadButton.RightBumper, values[5]);
		SetButton(ref buttons, GamepadButton.Back, values[8]);
		SetButton(ref buttons, GamepadButton.Start, values[9]);
		SetButton(ref buttons, GamepadButton.LeftStickThumb, values[10]);
		SetButton(ref buttons, GamepadButton.RightStickThumb, values[11]);
		SetButton(ref buttons, GamepadButton.DpadUp, values[12]);
		SetButton(ref buttons, GamepadButton.DpadDown, values[13]);
		SetButton(ref buttons, GamepadButton.DpadLeft, values[14]);
		SetButton(ref buttons, GamepadButton.DpadRight, values[15]);

		// Standard mapping: axis Y is positive downward. GamepadState treats positive Y as up.
		reading = new StandardReading(
			buttons,
			ToThumb(values[16], false),
			ToThumb(values[17], true),
			ToThumb(values[18], false),
			ToThumb(values[19], true),
			ToTrigger(values[6]),
			ToTrigger(values[7]));
		return true;
	}

	private static void SetButton(ref GamepadButton buttons, GamepadButton flag, double value)
	{
		if (value >= 0.5d)
		{
			buttons |= flag;
		}
	}

	private static short ToThumb(double axis, bool invert)
	{
		if (invert)
		{
			axis = -axis;
		}

		if (double.IsNaN(axis))
		{
			return 0;
		}

		if (axis > 1d)
		{
			axis = 1d;
		}
		else if (axis < -1d)
		{
			axis = -1d;
		}

		var scaled = axis >= 0d ? axis * 32767d : axis * 32768d;
		var rounded = (int) Math.Round(scaled);
		if (rounded > short.MaxValue)
		{
			return short.MaxValue;
		}

		if (rounded < short.MinValue)
		{
			return short.MinValue;
		}

		return (short) rounded;
	}

	private static byte ToTrigger(double value)
	{
		if (double.IsNaN(value) || value <= 0d)
		{
			return 0;
		}

		if (value >= 1d)
		{
			return 255;
		}

		return (byte) Math.Round(value * 255d);
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

	private readonly struct StandardReading
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
	}

	#endregion
}

internal static partial class BrowserGamepadInterop
{
	// Same module name CornerstoneModule.ImportMain loads before the app runs.
	private const string ModuleName = "cornerstone";

	[JSImport("GamepadHelper.read", ModuleName)]
	internal static partial string Read(int index);
}
