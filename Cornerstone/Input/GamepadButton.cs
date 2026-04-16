#region References

using System;

#endregion

namespace Cornerstone.Input;

[Flags]
public enum GamepadButton : uint
{
	None = 0x0000,
	DpadUp = 0x0001,
	DpadDown = 0x0002,
	DpadLeft = 0x0004,
	DpadRight = 0x0008,
	Start = 0x0010,
	Back = 0x0020,
	LeftStickThumb = 0x0040,
	RightStickThumb = 0x0080,
	LeftBumper = 0x0100,
	RightBumper = 0x0200,
	LeftTrigger = 0x0400,
	RightTrigger = 0x0800,
	A = 0x1000,
	B = 0x2000,
	X = 0x4000,
	Y = 0x8000,

	// These are custom, do not line up with xinput
	LeftStickLeft = 0x100000,
	LeftStickRight = 0x200000,
	LeftStickUp = 0x400000,
	LeftStickDown = 0x800000,
	RightStickLeft = 0x1000000,
	RightStickRight = 0x2000000,
	RightStickUp = 0x4000000,
	RightStickDown = 0x8000000
}