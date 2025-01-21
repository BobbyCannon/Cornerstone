#region References

using System.Runtime.InteropServices;

#endregion

namespace Cornerstone.Windows.Native;

#region Classes

internal static class NativeXInput
{
	#region Constants

	public const uint ErrorDeviceNotConnected = 0x048F;
	public const int XinputGamepadA = 0x1000;
	public const int XinputGamepadB = 0x2000;
	public const int XinputGamepadBack = 0x0020;
	public const int XinputGamepadDpadDown = 0x0002;
	public const int XinputGamepadDpadLeft = 0x0004;
	public const int XinputGamepadDpadRight = 0x0008;
	public const int XinputGamepadDpadUp = 0x0001;
	public const int XinputGamepadLeftShoulder = 0x0100;
	public const int XinputGamepadLeftThumb = 0x0040;
	public const int XinputGamepadRightShoulder = 0x0200;
	public const int XinputGamepadRightThumb = 0x0080;
	public const int XinputGamepadStart = 0x0010;
	public const int XinputGamepadX = 0x4000;
	public const int XinputGamepadY = 0x8000;

	private const string XInputDll = "xinput1_4.dll";

	#endregion

	#region Methods

	[DllImport(XInputDll)]
	public static extern void XInputEnable(bool enable);

	[DllImport(XInputDll)]
	public static extern uint XInputGetAudioDeviceIds(uint dwUserIndex, [MarshalAs(UnmanagedType.LPWStr)] out string pRenderDeviceId, ref uint pRenderCount, [MarshalAs(UnmanagedType.LPWStr)] out string pCaptureDeviceId, ref uint pCaptureCount);

	[DllImport(XInputDll)]
	public static extern uint XInputGetBatteryInformation(uint dwUserIndex, byte devType, ref XinputBatteryInformation pBatteryInformation);

	[DllImport(XInputDll)]
	public static extern uint XInputGetCapabilities(uint dwUserIndex, uint dwFlags, ref XinputCapabilities pCapabilities);

	[DllImport(XInputDll)]
	public static extern uint XInputGetKeystroke(uint dwUserIndex, uint dwReserved, ref XinputKeystroke pKeystroke);

	[DllImport(XInputDll)]
	public static extern uint XInputGetState(uint dwUserIndex, ref XinputState pState);

	[DllImport(XInputDll)]
	public static extern uint XInputSetState(uint dwUserIndex, ref XinputVibration pVibration);

	#endregion

	#region Structures

	/// <summary>
	/// https://learn.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_state
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct XinputState
	{
		/// <summary>
		/// State packet number. The packet number indicates whether there have been any
		/// changes in the state of the controller. If the dwPacketNumber member is the
		/// same in sequentially returned XINPUT_STATE structures, the controller state
		/// has not changed.
		/// </summary>
		[FieldOffset(0)]
		public uint dwPacketNumber;

		[FieldOffset(4)]
		public XinputGamepad Gamepad;
	}

	/// <summary>
	/// https://learn.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_gamepad
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct XinputGamepad
	{
		[MarshalAs(UnmanagedType.I2)]
		[FieldOffset(0)]
		public ushort wButtons;

		[MarshalAs(UnmanagedType.I1)]
		[FieldOffset(2)]
		public byte bLeftTrigger;

		[MarshalAs(UnmanagedType.I1)]
		[FieldOffset(3)]
		public byte bRightTrigger;

		[MarshalAs(UnmanagedType.I2)]
		[FieldOffset(4)]
		public short sThumbLX;

		[MarshalAs(UnmanagedType.I2)]
		[FieldOffset(6)]
		public short sThumbLY;

		[MarshalAs(UnmanagedType.I2)]
		[FieldOffset(8)]
		public short sThumbRX;

		[MarshalAs(UnmanagedType.I2)]
		[FieldOffset(10)]
		public short sThumbRY;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct XinputVibration
	{
		[MarshalAs(UnmanagedType.I2)]
		public ushort wLeftMotorSpeed;

		[MarshalAs(UnmanagedType.I2)]
		public ushort wRightMotorSpeed;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct XinputCapabilities
	{
		[MarshalAs(UnmanagedType.I1)]
		[FieldOffset(0)]
		public byte Type;

		[MarshalAs(UnmanagedType.I1)]
		[FieldOffset(1)]
		public byte SubType;

		[MarshalAs(UnmanagedType.I2)]
		[FieldOffset(2)]
		public ushort Flags;

		[FieldOffset(4)]
		public XinputGamepad Gamepad;

		[FieldOffset(16)]
		public XinputVibration Vibration;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct XinputBatteryInformation
	{
		[MarshalAs(UnmanagedType.I1)]
		[FieldOffset(0)]
		public byte BatteryType;

		[MarshalAs(UnmanagedType.I1)]
		[FieldOffset(1)]
		public byte BatteryLevel;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct XinputKeystroke
	{
		[MarshalAs(UnmanagedType.I2)]
		[FieldOffset(0)]
		public ushort VirtualKey;

		[MarshalAs(UnmanagedType.I2)]
		[FieldOffset(2)]
		public char Unicode;

		[MarshalAs(UnmanagedType.I2)]
		[FieldOffset(4)]
		public ushort Flags;

		[MarshalAs(UnmanagedType.I2)]
		[FieldOffset(5)]
		public byte UserIndex;

		[MarshalAs(UnmanagedType.I1)]
		[FieldOffset(6)]
		public byte HidCode;
	}

	#endregion
}

#endregion