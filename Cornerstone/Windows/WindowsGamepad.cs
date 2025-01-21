#region References

using System;
using System.Linq;
using System.Threading;
using Cornerstone.Attributes;
using Cornerstone.Extensions;
using Cornerstone.Input;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using static Cornerstone.Windows.Native.NativeXInput;

#endregion

namespace Cornerstone.Windows;

public class WindowsGamepad : Gamepad
{
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private uint _lastPacketNumber;
	private XinputState _xinputState;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public WindowsGamepad(
		IDateTimeProvider dateTimeProvider,
		WeakEventManager weakEventManager,
		IDispatcher dispatcher)
		: base(weakEventManager, dispatcher)
	{
		_dateTimeProvider = dateTimeProvider;
		_lastPacketNumber = 0;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void Work(TimeSpan elapsed)
	{
		var result = XInputGetState(0, ref _xinputState);
		if (result == ErrorDeviceNotConnected)
		{
			if (State.IsConnected)
			{
				State.Reset(_dateTimeProvider.UtcNow);
				OnChanged(State.ShallowClone());
			}

			Thread.Sleep(100);
			return;
		}

		if (_lastPacketNumber == _xinputState.dwPacketNumber)
		{
			Thread.Sleep(10);
			return;
		}

		_lastPacketNumber = _xinputState.dwPacketNumber;
		var lastButtons = State.Buttons;

		State.IsConnected = true;
		State.LeftTriggerValue = _xinputState.Gamepad.bLeftTrigger;
		State.RightTriggerValue = _xinputState.Gamepad.bRightTrigger;
		State.UpdateLeftThumb(_xinputState.Gamepad.sThumbLX, _xinputState.Gamepad.sThumbLY);
		State.UpdateRightThumb(_xinputState.Gamepad.sThumbRX, _xinputState.Gamepad.sThumbRY);
		State.Buttons = (GamepadButton) _xinputState.Gamepad.wButtons;

		if (State.LeftTriggerPercent >= 0.5)
		{
			State.Buttons = State.Buttons.SetFlag(GamepadButton.LeftTrigger);
		}
		
		if (State.RightTriggerPercent >= 0.5)
		{
			State.Buttons = State.Buttons.SetFlag(GamepadButton.RightTrigger);
		}
		
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

		Thread.Sleep(10);
	}

	private void TriggerButtonEvents(GamepadButton lastButtons, GamepadButton stateButtons)
	{
		var oldButtons = lastButtons.GetFlaggedValues();
		var newButtons = stateButtons.GetFlaggedValues();

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