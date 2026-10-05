#region References

using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Input;
using Cornerstone.Runtime;
using static Cornerstone.Platforms.Windows.Native.NativeXInput;

#endregion

namespace Cornerstone.Platforms.Windows;

public class WindowsGamepad : Gamepad
{
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private uint _lastPacketNumber;
	private XinputState _xinputState;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public WindowsGamepad(IDateTimeProvider dateTimeProvider) : base(dateTimeProvider)
	{
		_dateTimeProvider = dateTimeProvider;
		_lastPacketNumber = 0;
	}

	#endregion

	#region Methods

	public bool ShouldUpdate()
	{
		return true;
	}

	public override void Update()
	{
		var result = XInputGetState((uint) State.Index, ref _xinputState);
		if (result == ErrorDeviceNotConnected)
		{
			if (State.IsConnected)
			{
				State.Reset(_dateTimeProvider.UtcNow);
				OnChanged(State.ShallowClone());
			}
			return;
		}

		if (_lastPacketNumber == _xinputState.dwPacketNumber)
		{
			return;
		}

		_lastPacketNumber = _xinputState.dwPacketNumber;
		var lastButtons = State.Buttons;

		State.IsConnected = true;
		State.LeftTriggerValue = _xinputState.Gamepad.bLeftTrigger;
		State.RightTriggerValue = _xinputState.Gamepad.bRightTrigger;
		State.Buttons = (GamepadButton) _xinputState.Gamepad.wButtons;
		State.UpdateLeftThumb(_xinputState.Gamepad.sThumbLX, _xinputState.Gamepad.sThumbLY);
		State.UpdateRightThumb(_xinputState.Gamepad.sThumbRX, _xinputState.Gamepad.sThumbRY);
		State.UpdateTriggers();

		// https://learn.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_gamepad

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

	private void TriggerButtonEvents(GamepadButton lastButtons, GamepadButton stateButtons)
	{
		var oldButtons = lastButtons.GetFlagValues();
		var newButtons = stateButtons.GetFlagValues();

		foreach (var released in oldButtons.Except(newButtons))
		{
			// Released
			OnButtonChanged(released, false);
		}

		foreach (var released in newButtons.Except(oldButtons))
		{
			// Pressed
			OnButtonChanged(released, true);
		}
	}

	#endregion
}