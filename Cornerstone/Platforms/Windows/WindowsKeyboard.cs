#region References

using System;
using System.Linq;
using Cornerstone.Input;
using Cornerstone.Runtime;
using Cornerstone.Windows.Inputs;
using Cornerstone.Windows.Native;

#endregion

namespace Cornerstone.Platforms.Windows;

/// <summary>
/// Represents the keyboard and allows for simulated input.
/// </summary>
public class WindowsKeyboard : Keyboard
{
	#region Fields

	private NativeInput.KeyboardHookDelegate _monitorCallback;
	private IntPtr _monitorHandle;

	#endregion

	#region Properties

	public override bool IsMonitoring => _monitorHandle != IntPtr.Zero;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool IsKeyDown(params KeyboardKey[] keys)
	{
		return keys.Aggregate(false, (current, key) => current | (NativeInput.GetKeyState((ushort) key) < 0));
	}

	/// <inheritdoc />
	public override bool IsKeyUp(params KeyboardKey[] keys)
	{
		return !IsKeyDown(keys);
	}

	/// <inheritdoc />
	public override bool IsTogglingKeyInEffect(KeyboardKey key)
	{
		var result = NativeInput.GetKeyState((ushort) key);
		return (result & 0x01) == 0x01;
	}

	/// <inheritdoc />
	public override InputBuilder SendInput(InputBuilder builder, TimeSpan delay)
	{
		return WindowsInput.SendInput(builder, delay);
	}

	/// <inheritdoc />
	public override Keyboard StartMonitoring()
	{
		if (IsMonitoring)
		{
			return this;
		}

		const int lowLevelKeyboardHook = 13;

		// https://gist.github.com/erisonliang/aa740d6694768fe86594565c3a3c6405
		// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexa

		_monitorCallback = HookCallback;
		_monitorHandle = NativeInput.SetWindowsHookEx(lowLevelKeyboardHook, _monitorCallback, IntPtr.Zero, 0);

		return this;
	}

	/// <inheritdoc />
	public override Keyboard StopMonitoring()
	{
		if (_monitorHandle != IntPtr.Zero)
		{
			NativeInput.UnhookWindowsHookEx(_monitorHandle);
			_monitorHandle = IntPtr.Zero;
		}

		return this;
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder()
	{
		return new WindowsInputBuilder();
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder(KeyStroke[] keyStrokes)
	{
		return new WindowsInputBuilder(keyStrokes);
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder(KeyboardModifier modifier, KeyboardKey[] keys)
	{
		return new WindowsInputBuilder(modifier, keys);
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder(string text, bool textInputAsKeyPresses)
	{
		return new WindowsInputBuilder(text, textInputAsKeyPresses);
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder(KeyboardKey[] keys)
	{
		return new WindowsInputBuilder(keys);
	}

	/// <inheritdoc />
	protected override InputBuilder SendInput(InputBuilder builder)
	{
		return WindowsInput.SendInput(builder);
	}

	private KeyboardEvent GetEvent(WindowsMessage wParam)
	{
		return wParam switch
		{
			WindowsMessage.KeyDown => KeyboardEvent.KeyDown,
			WindowsMessage.KeyUp => KeyboardEvent.KeyUp,
			WindowsMessage.SystemKeyDown => KeyboardEvent.SystemKeyDown,
			WindowsMessage.SystemKeyUp => KeyboardEvent.SystemKeyUp,
			_ => KeyboardEvent.Unknown
		};
	}

	private int HookCallback(int code, int wParam, ref KeyboardInput lParam)
	{
		State.DateTime = DateTimeProvider.RealTime.UtcNow;
		State.Event = GetEvent((WindowsMessage) wParam);

		if (code >= 0)
		{
			var key = (KeyboardKey) lParam.VirtualKeyCode;
			var keyEvent = (WindowsMessage) wParam;
			var isPressed = keyEvent
				is WindowsMessage.KeyDown
				or WindowsMessage.SystemKeyDown;

			switch (key)
			{
				case KeyboardKey.CapsLock:
				{
					State.IsCapsLockOn = IsTogglingKeyInEffect(KeyboardKey.CapsLock);
					break;
				}
				case KeyboardKey.Alt:
				{
					State.IsAltPressed = isPressed;
					State.IsLeftAltPressed = isPressed;
					State.IsRightAltPressed = isPressed;
					break;
				}
				case KeyboardKey.LeftAlt:
				{
					State.IsAltPressed = isPressed;
					State.IsLeftAltPressed = isPressed;
					break;
				}
				case KeyboardKey.RightAlt:
				{
					State.IsAltPressed = isPressed;
					State.IsRightAltPressed = isPressed;
					break;
				}
				case KeyboardKey.Control:
				{
					State.IsControlPressed = isPressed;
					State.IsLeftControlPressed = isPressed;
					State.IsRightControlPressed = isPressed;
					break;
				}
				case KeyboardKey.LeftControl:
				{
					State.IsControlPressed = isPressed;
					State.IsLeftControlPressed = isPressed;
					break;
				}
				case KeyboardKey.RightControl:
				{
					State.IsControlPressed = isPressed;
					State.IsRightControlPressed = isPressed;
					break;
				}
				case KeyboardKey.Shift:
				{
					State.IsShiftPressed = isPressed;
					State.IsLeftShiftPressed = isPressed;
					State.IsRightShiftPressed = isPressed;
					break;
				}
				case KeyboardKey.LeftShift:
				{
					State.IsShiftPressed = isPressed;
					State.IsLeftShiftPressed = isPressed;
					break;
				}
				case KeyboardKey.RightShift:
				{
					State.IsShiftPressed = isPressed;
					State.IsRightShiftPressed = isPressed;
					break;
				}
			}

			var character = ToCharacter(key, State);

			if (key == KeyboardKey.Packet)
			{
				var scanCode = (char) lParam.ScanCode;
				key = (KeyboardKey) (NativeInput.VkKeyScan(scanCode) & 0xFF);
			}

			State.Character = character;
			State.Key = key;
			State.IsPressed = isPressed;

			var pressed = new KeyboardStateArg();
			pressed.UpdateWith(State);
			OnKeyPressed(pressed);

			if (pressed.IsHandled)
			{
				return 1;
			}

			//Debug.WriteLine(pressed.ToCSharp(new CodeWriterSettings { TextFormat = TextFormat.Spaced, IgnoreDefaultValues = true, IgnoreNullValues = true }) + ",");
			//Debug.WriteLine($"Code: {code}, wParam: {wParam}, lParam.vkCode: {lParam.vkCode}; flags: {lParam.flags}");
		}

		return NativeInput.CallNextHookEx(_monitorHandle, code, wParam, ref lParam);
	}

	#endregion
}